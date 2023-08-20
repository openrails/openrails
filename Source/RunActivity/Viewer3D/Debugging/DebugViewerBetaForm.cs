using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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

namespace Orts.Viewer3D.Debugging
{
    public partial class DispatchViewerBeta : Form
    {
        #region Variables
        /// <summary>
        /// Reference to the main simulator object.
        /// </summary>
        public readonly Simulator simulator;
        private MapDataProvider MapDataProvider;
        /// <summary>
        /// Used to periodically check if we should shift the view when the
        /// user is holding down a "shift view" button.
        /// </summary>
        private Timer UITimer;
        public Viewer Viewer;

        private int IM_Width;
        private int IM_Height;

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
        TrackNode[] nodes;

        public List<Train> selectedTrainList;
        /// <summary>
        /// contains the last position of the mouse
        /// </summary>
        private System.Drawing.Point LastCursorPosition = new System.Drawing.Point();
        public Pen redPen = new Pen(Color.FromArgb(244, 67, 54));
        public Pen greenPen = new Pen(Color.FromArgb(76, 175, 80));
        public Pen orangePen = new Pen(Color.FromArgb(255, 235, 59));
        public Pen trainPen = new Pen(Color.DarkGreen);
        public Pen pathPen = new Pen(Color.FromArgb(52, 152, 219));
        public Pen grayPen = new Pen(Color.Gray);
        public Pen PlatformPen = new Pen(Color.Blue);
        public Pen TrackPen = new Pen(Color.FromArgb(46, 64, 83));
        public Pen ZoomTargetPen = new Pen(Color.FromArgb(46, 64, 83));
        // the train selected by leftclicking the mouse
        public Train PickedTrain;
        /// <summary>
        /// Defines the area to view, in meters. The left edge is meters from the leftmost extent of the route.
        /// </summary>
        public RectangleF ViewWindow;

        // Extents of the route in meters measured from the World origin
        public float minX = float.MaxValue;
        public float minY = float.MaxValue;
        public float maxX = float.MinValue;
        public float maxY = float.MinValue;

        public int RedrawCount;
        public Font trainFont = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
        public Font sidingFont = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
        public Font PlatformFont = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
        public Font SignalFont = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
        private SolidBrush trainBrush = new SolidBrush(Color.Red);
        public SolidBrush sidingBrush = new SolidBrush(Color.Blue);
        public SolidBrush PlatformBrush = new SolidBrush(Color.DarkBlue);
        public SolidBrush SignalBrush = new SolidBrush(Color.DarkRed);
        public SolidBrush InactiveTrainBrush = new SolidBrush(Color.DarkRed);

        private double lastUpdateTime;

        private bool MapCustomizationVisible = false;
        #endregion

        public DispatchViewerBeta(Simulator simulator, Viewer viewer)
        {
            InitializeComponent();

            if (simulator == null)
                throw new ArgumentNullException("simulator", "Simulator object cannot be null.");

            this.simulator = simulator;
            Viewer = viewer;
            MapDataProvider = new MapDataProvider(this);
            nodes = simulator.TDB.TrackDB.TrackNodes;

            InitializeForm();

            ViewWindow = new RectangleF(0, 0, 5000f, 5000f);
            mapResolutionUpDown.Accelerations.Add(new NumericUpDownAcceleration(1, 100));
            /*boxSetSignal.Items.Add("System Controlled");
            boxSetSignal.Items.Add("Stop");
            boxSetSignal.Items.Add("Approach");
            boxSetSignal.Items.Add("Proceed");
            chkAllowUserSwitch.Checked = false;*/
            selectedTrainList = new List<Train>();

            InitializeData();
            InitializeImage();

            // Initialise the timer used to handle user input
            UITimer = new Timer();
            UITimer.Interval = 100;
            UITimer.Tick += new EventHandler(UITimer_Tick);
            UITimer.Start();
        }

        void InitializeForm()
        {
            if (MPManager.IsMultiPlayer() && MPManager.IsServer())
            {
                playerRolePanel.Visible = true;
                messagesPanel.Visible = true;
                multiplayerSettingsPanel.Visible = true;
            }

            float[] dashPattern = { 4, 2 };
            ZoomTargetPen.DashPattern = dashPattern;
            pathPen.DashPattern = dashPattern;
        }

        #region initData
        private void InitializeData()
        {
            /*if (!loaded)
            {
                // do this only once
                loaded = true;
                //trackSections.DataSource = new List<InterlockingTrack>(simulator.InterlockingSystem.Tracks.Values).ToArray();
                Localizer.Localize(this, Viewer.Catalog);
            }*/

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
            mapResolutionUpDown.Maximum = (decimal)maxsize;
            Inited = true;

            if (simulator.TDB == null || simulator.TDB.TrackDB == null || simulator.TDB.TrackDB.TrItemTable == null)
                return;

            MapDataProvider.PopulateItemLists();
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
        public void InitializeImage()
        {
            /*mapCanvas.Width = IM_Width;
            mapCanvas.Height = IM_Height;*/

            if (mapCanvas.Image != null)
            {
                mapCanvas.Image.Dispose();
            }

            mapCanvas.Image = new Bitmap(mapCanvas.Width, mapCanvas.Height);
            /*imageList1 = new ImageList();
            playersView.View = View.LargeIcon;
            imageList1.ImageSize = new Size(64, 64);
            playersView.LargeImageList = imageList1;*/
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
            IM_Width = mapCanvas.Width;
            IM_Height = mapCanvas.Height;

            if (mapCanvas.Image != null)
            {
                mapCanvas.Image.Dispose();
            }

            mapCanvas.Image = new Bitmap(mapCanvas.Width, mapCanvas.Height);
            /*if (Height < 600 || Width < 800) return;
            if (oldHeight != Height || oldWidth != label1.Left)//use the label "Res" as anchor point to determine the picture size
            {
                oldWidth = label1.Left; oldHeight = Height;
                IM_Width = label1.Left - 20;
                IM_Height = Height - mapCanvas.Top;
                mapCanvas.Width = IM_Width;
                mapCanvas.Height = Height - mapCanvas.Top - 40;
                if (mapCanvas.Image != null)
                {
                    mapCanvas.Image.Dispose();
                }

                mapCanvas.Image = new Bitmap(mapCanvas.Width, mapCanvas.Height);

                if (btnAssist.Left - 10 < composeMSG.Right)
                {
                    var size = composeMSG.Width;
                    composeMSG.Left = msgAll.Left = msgSelected.Left = reply2Selected.Left = btnAssist.Left - 10 - size;
                    MSG.Width = messages.Width = composeMSG.Left - 20;
                    MSG.Width = messages.Width = composeMSG.Left - 20;
                }
                firstShow = true;
            }*/
        }

        /// <summary>
        /// Regenerates the 2D view. At the moment, examines the track network
        /// each time the view is drawn. Later, the traversal and drawing can be separated.
        /// </summary>
        public void GenerateView(bool dragging = false)
        {
            if (!Inited) return;

            timeLabel.Visible = showTimeCheckbox.Checked;
            if (showTimeCheckbox.Checked)
                MapDataProvider.ShowSimulationTime();

            if (mapCanvas.Image == null) InitializeImage();
            DetermineLocations();

            /*if (firstShow)
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
            }*/
            if (firstShow || followTrain)
            {
                //see who should I look at:
                //if the player is selected in the avatar list, show the player, otherwise, show the one with the lowest index
                WorldPosition pos = Program.Simulator.PlayerLocomotive == null ? Program.Simulator.Trains.First().Cars.First().WorldPosition : Program.Simulator.PlayerLocomotive.WorldPosition;
                if (playersView.SelectedIndices.Count > 0 && !playersView.SelectedIndices.Contains(0))
                {
                    int i = playersView.SelectedIndices.Cast<int>().Min();
                    string name = (playersView.Items[i].Text ?? "").Split(' ').First().Trim();
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

            using (Graphics g = Graphics.FromImage(mapCanvas.Image))
            {
                // Optional anti-aliasing
                if (useAntiAliasingCheckbox.Checked == true)
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                subX = minX + ViewWindow.X; subY = minY + ViewWindow.Y;
                g.Clear(Color.White);

                xScale = mapCanvas.Width / ViewWindow.Width;
                yScale = mapCanvas.Height / ViewWindow.Height;
                xScale = yScale = Math.Max(xScale, yScale); // Make X and Y scales the same to maintain correct angles

                // Set the default pen to represent 1 meter
                var scale = (float)Math.Round((double)xScale);  // Round to nearest pixels/meter
                var penWidth = (int)MathHelper.Clamp(scale, 1, 4);  // Keep 1 <= width <= 4 pixels

                PointF[] points = new PointF[3];
                Pen p = grayPen;

                // TODO: Refactor
                p.Width = MathHelper.Clamp(xScale, 1, 3);
                greenPen.Width = orangePen.Width = redPen.Width = p.Width; pathPen.Width = 2 * p.Width;
                trainPen.Width = p.Width * 6;

                grayPen.Width = greenPen.Width = orangePen.Width = redPen.Width = penWidth;
                pathPen.Width = penWidth * 2;

                var forwardDist = 100 / xScale; if (forwardDist < 5) forwardDist = 5;

                /*PointF scaledA = new PointF(0, 0);
                PointF scaledB = new PointF(0, 0);
                PointF scaledC = new PointF(0, 0);*/

                // Draw platforms first because track is drawn over the thicker platform line
                DrawPlatforms(g, penWidth);

                PointF scaledA, scaledB;
                DrawTrack(g, p, out scaledA, out scaledB);

                if (Dragging == false)
                {
                    // Draw trains and path
                    DrawTrains(g, scaledA, scaledB);

                    // Keep widgetWidth <= 15 pixels
                    var widgetWidth = Math.Min(penWidth * 6, 15);

                    // Draw signals on top of path so they are easier to see.
                    signalItemsDrawn.Clear();
                    ShowSignals(g, scaledB, widgetWidth);

                    // Draw switches
                    switchItemsDrawn.Clear();
                    ShowSwitches(g, widgetWidth);

                    // Draw labels for sidings and platforms last so they go on top for readability
                    MapDataProvider.CleanTextCells();  // Empty the listing of labels ready for adding labels again
                    ShowPlatformLabels(g); // Platforms take priority over sidings and signal states
                    ShowSidingLabels(g);
                }

                DrawZoomTarget(g);

                /*
                foreach (var line in segments)
                {

                    scaledA.X = (line.A.TileX * 2048 - subX + (float)line.A.X) * xScale; scaledA.Y = mapCanvas.Height - (line.A.TileZ * 2048 - subY + (float)line.A.Z) * yScale;
                    scaledB.X = (line.B.TileX * 2048 - subX + (float)line.B.X) * xScale; scaledB.Y = mapCanvas.Height - (line.B.TileZ * 2048 - subY + (float)line.B.Z) * yScale;

                    if ((scaledA.X < 0 && scaledB.X < 0) || (scaledA.X > mapCanvas.Width && scaledB.X > IM_Width) || (scaledA.Y > IM_Height && scaledB.Y > IM_Height) || (scaledA.Y < 0 && scaledB.Y < 0))
                        continue;

                    if (line.isCurved == true)
                    {
                        scaledC.X = ((float)line.C.X - subX) * xScale; scaledC.Y = mapCanvas.Height - ((float)line.C.Z - subY) * yScale;
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

                    x = (sw.Location.X - subX) * xScale; y = mapCanvas.Height - (sw.Location.Y - subY) * yScale;

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
                    x = (s.Location.X - subX) * xScale; y = mapCanvas.Height - (s.Location.Y - subY) * yScale;
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
                            scaledB.X = (s.Dir.X - subX) * xScale; scaledB.Y = mapCanvas.Height - (s.Dir.Y - subY) * yScale;
                            g.DrawLine(pen, scaledItem, scaledB);
                        }
                    }
                }
                */
                /*if (true)
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
                    var chosen = playersView.SelectedItems;
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
                            scaledItem.Y = mapCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;
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
                        x = (loc.TileX * 2048 + loc.Location.X - subX) * xScale; y = mapCanvas.Height - (loc.TileZ * 2048 + loc.Location.Z - subY) * yScale;
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
                                x = (t1.TileX * 2048 + t1.Location.X - subX) * xScale; y = mapCanvas.Height - (t1.TileZ * 2048 + t1.Location.Z - subY) * yScale;
                                //x = (worldPos.TileX * 2048 + worldPos.Location.X - minX - ViewWindow.X) * xScale; y = pictureBox1.Height - (worldPos.TileZ * 2048 + worldPos.Location.Z - minY - ViewWindow.Y) * yScale;
                                if (x < -margin || x > IM_Width + margin || y > IM_Height + margin || y < -margin) continue;

                                scaledItem.X = x; scaledItem.Y = y;

                                t1.Move(-car.CarLengthM);
                                x = (t1.TileX * 2048 + t1.Location.X - subX) * xScale; y = mapCanvas.Height - (t1.TileZ * 2048 + t1.Location.Z - subY) * yScale;
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
                        scaledItem.Y = -25 + mapCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;

                        g.DrawString(name, trainFont, trainBrush, scaledItem);

                    }
                    if (switchPickedItemHandled)
                        switchPickedItem = null;
                    if (signalPickedItemHandled)
                        signalPickedItem = null;
                }*/
            }

            mapCanvas.Invalidate(); // Triggers a re-paint
        }

        private void DrawPlatforms(Graphics g, int penWidth)
        {
            if (!showPlatformsCheckbox.Checked)
                return;

            // Platforms can be obtrusive, so draw in solid blue only when zoomed in and fade them as we zoom out
            switch (penWidth)
            {
                case 1:
                    PlatformPen.Color = Color.FromArgb(174, 214, 241); break;
                case 2:
                    PlatformPen.Color = Color.FromArgb(93, 173, 226); break;
                default:
                    PlatformPen.Color = Color.FromArgb(46, 134, 193); break;
            }

            var width = grayPen.Width * 3;
            PlatformPen.Width = width;
            foreach (var p in platforms)
            {
                var scaledA = new PointF((p.Extent1.X - subX) * xScale, mapCanvas.Height - (p.Extent1.Y - subY) * yScale);
                var scaledB = new PointF((p.Extent2.X - subX) * xScale, mapCanvas.Height - (p.Extent2.Y - subY) * yScale);

                MapDataProvider.FixForBadData(width, ref scaledA, ref scaledB, p.Extent1, p.Extent2);
                g.DrawLine(PlatformPen, scaledA, scaledB);
            }
        }

        private void DrawTrack(Graphics g, Pen p, out PointF scaledA, out PointF scaledB)
        {

            PointF[] points = new PointF[3];
            scaledA = new PointF(0, 0);
            scaledB = new PointF(0, 0);
            PointF scaledC = new PointF(0, 0);
            foreach (var line in segments)
            {
                scaledA.X = (line.A.TileX * 2048 - subX + (float)line.A.X) * xScale;
                scaledA.Y = mapCanvas.Height - (line.A.TileZ * 2048 - subY + (float)line.A.Z) * yScale;
                scaledB.X = (line.B.TileX * 2048 - subX + (float)line.B.X) * xScale;
                scaledB.Y = mapCanvas.Height - (line.B.TileZ * 2048 - subY + (float)line.B.Z) * yScale;

                if ((scaledA.X < 0 && scaledB.X < 0)
                    || (scaledA.Y < 0 && scaledB.Y < 0))
                    continue;

                if (line.isCurved == true)
                {
                    scaledC.X = ((float)line.C.X - subX) * xScale; scaledC.Y = mapCanvas.Height - ((float)line.C.Z - subY) * yScale;
                    points[0] = scaledA; points[1] = scaledC; points[2] = scaledB;
                    g.DrawCurve(TrackPen, points);
                }
                else g.DrawLine(TrackPen, scaledA, scaledB);
            }
        }

        private void DrawTrains(Graphics g, PointF scaledA, PointF scaledB)
        {
            var margin = 30 * xScale;   //margins to determine if we want to draw a train
            var margin2 = 5000 * xScale;

            //variable for drawing train path
            var mDist = 5000f; var pDist = 50; //segment length when drawing path

            selectedTrainList.Clear();

            if (simulator.TimetableMode)
            {
                // Add the player's train
                if (simulator.PlayerLocomotive.Train is Orts.Simulation.AIs.AITrain)
                    selectedTrainList.Add(simulator.PlayerLocomotive.Train as Orts.Simulation.AIs.AITrain);

                // and all the other trains
                foreach (var train in simulator.AI.AITrains)
                    selectedTrainList.Add(train);
            }
            else
            {
                foreach (var train in simulator.Trains)
                    selectedTrainList.Add(train);
            }

            foreach (var train in selectedTrainList)
            {
                string trainName;
                WorldPosition worldPos;
                TrainCar locoCar = null;
                if (train.LeadLocomotive != null)
                {
                    trainName = train.GetTrainName(train.LeadLocomotive.CarID);
                    locoCar = train.LeadLocomotive;
                }
                else if (train.Cars != null && train.Cars.Count > 0)
                {
                    trainName = train.GetTrainName(train.Cars[0].CarID);
                    if (train.TrainType == Train.TRAINTYPE.AI)
                        trainName = train.Number.ToString() + ":" + train.Name;

                    locoCar = train.Cars.Where(r => r is MSTSLocomotive).FirstOrDefault();

                    // Skip trains with no loco
                    if (locoCar == null)
                        continue;
                }
                else
                    continue;

                // Draw the path, then each car of the train, then maybe the name
                var loc = train.FrontTDBTraveller.WorldLocation;
                float x = (loc.TileX * 2048 + loc.Location.X - subX) * xScale;
                float y = mapCanvas.Height - (loc.TileZ * 2048 + loc.Location.Z - subY) * yScale;

                // If train out of view then skip it.
                if (x < -margin2
                    || y < -margin2)
                    continue;

                DrawTrainPath(train, subX, subY, pathPen, g, scaledA, scaledB, pDist, mDist);

                // If zoomed out, so train occupies less than 2 * minTrainPx pixels, then 
                // draw the train as 2 squares of combined length minTrainPx.
                const int minTrainPx = 24;

                // pen | train | Values for a good presentation
                //  1		10
                //  2       12
                //  3       14
                //  4		16
                trainPen.Width = grayPen.Width * 6;

                var minTrainLengthM = minTrainPx / xScale; // Calculate length equivalent to a set number of pixels
                bool drawEveryCar = IsDrawEveryCar(train, minTrainLengthM);

                foreach (var car in train.Cars)
                    DrawCar(g, train, car, locoCar, margin, minTrainPx, drawEveryCar);

                worldPos = locoCar.WorldPosition;
                var scaledTrain = new PointF();
                scaledTrain.X = (worldPos.TileX * 2048 - subX + worldPos.Location.X) * xScale;
                scaledTrain.Y = -25 + mapCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;
                if (showTrainLabelsCheckbox.Checked)
                    DrawTrainLabels(g, train, trainName, locoCar, scaledTrain);
            }
        }

        private void DrawCar(Graphics g, Train train, TrainCar car, TrainCar locoCar, float margin, int minTrainPx, bool drawEveryCar)
        {
            if (drawEveryCar == false)
                // Skip the intermediate cars
                if (car != train.Cars.First() && car != train.Cars.Last())
                    return;

            var t = new Traveller(train.RearTDBTraveller);
            var worldPos = car.WorldPosition;
            var dist = t.DistanceTo(worldPos.WorldLocation.TileX, worldPos.WorldLocation.TileZ, worldPos.WorldLocation.Location.X, worldPos.WorldLocation.Location.Y, worldPos.WorldLocation.Location.Z);
            if (dist > -1)
            {
                var scaledTrain = new PointF();
                float x;
                float y;
                if (drawEveryCar)
                {
                    t.Move(dist + car.CarLengthM / 2); // Move along from centre of car to front of car
                    x = (t.TileX * 2048 + t.Location.X - subX) * xScale;
                    y = mapCanvas.Height - (t.TileZ * 2048 + t.Location.Z - subY) * yScale;

                    // If car out of view then skip it.
                    if (x < -margin || y < -margin)
                        return;

                    t.Move(-car.CarLengthM + (1 / xScale)); // Move from front of car to rear less 1 pixel to create a visible gap
                    scaledTrain.X = x; scaledTrain.Y = y;
                }
                else // Draw the train as 2 boxes of fixed size
                {
                    trainPen.Width = minTrainPx / 2;
                    if (car == train.Cars.First())
                    {
                        // Draw first half a train back from the front of the first car as abox
                        t.Move(dist + car.CarLengthM / 2);
                        x = (t.TileX * 2048 + t.Location.X - subX) * xScale;
                        y = mapCanvas.Height - (t.TileZ * 2048 + t.Location.Z - subY) * yScale;

                        // If car out of view then skip it.
                        if (x < -margin || y < -margin)
                            return;

                        t.Move(-(minTrainPx - 2) / xScale / 2); // Move from front of car to rear less 1 pixel to create a visible gap
                    }
                    else // car == t.Cars.Last()
                    {
                        // Draw half a train back from the rear of the first box
                        worldPos = train.Cars.First().WorldPosition;
                        dist = t.DistanceTo(worldPos.WorldLocation.TileX, worldPos.WorldLocation.TileZ, worldPos.WorldLocation.Location.X, worldPos.WorldLocation.Location.Y, worldPos.WorldLocation.Location.Z);
                        t.Move(dist + train.Cars.First().CarLengthM / 2 - minTrainPx / xScale / 2);
                        x = (t.TileX * 2048 + t.Location.X - subX) * xScale;
                        y = mapCanvas.Height - (t.TileZ * 2048 + t.Location.Z - subY) * yScale;
                        if (x < -margin || y < -margin)
                            return;
                        t.Move(-minTrainPx / xScale / 2);
                    }
                    scaledTrain.X = x; scaledTrain.Y = y;
                }
                x = (t.TileX * 2048 + t.Location.X - subX) * xScale;
                y = mapCanvas.Height - (t.TileZ * 2048 + t.Location.Z - subY) * yScale;

                // If car out of view then skip it.
                if (x < -margin || y < -margin)
                    return;

                SetTrainColor(train, locoCar, car);
                g.DrawLine(trainPen, new PointF(x, y), scaledTrain);
            }
        }

        private void SetTrainColor(Train t, TrainCar locoCar, TrainCar car)
        {
            // Draw train in green with locos in brown
            // HSL values
            // Saturation: 100/100
            // Hue: if loco then H=50/360 else H=120/360
            // Lightness: if active then L=40/100 else L=30/100
            // RGB values
            // active loco: RGB 204,170,0
            // inactive loco: RGB 153,128,0
            // active car: RGB 0,204,0
            // inactive car: RGB 0,153,0
            if (MapDataProvider.IsActiveTrain(t as Simulation.AIs.AITrain))
                if (car is MSTSLocomotive)
                    trainPen.Color = (car == locoCar) ? Color.FromArgb(204, 170, 0) : Color.FromArgb(153, 128, 0);
                else
                    trainPen.Color = Color.FromArgb(0, 204, 0);
            else
                if (car is MSTSLocomotive)
                trainPen.Color = Color.FromArgb(153, 128, 0);
            else
                trainPen.Color = Color.FromArgb(0, 153, 0);

            // Draw player train with loco in red
            if (t.TrainType == Train.TRAINTYPE.PLAYER && car == locoCar)
                trainPen.Color = Color.Red;
        }

        private void DrawTrainLabels(Graphics g, Train t, string trainName, TrainCar firstCar, PointF scaledTrain)
        {
            WorldPosition worldPos = firstCar.WorldPosition;
            scaledTrain.X = (worldPos.TileX * 2048 - subX + worldPos.Location.X) * xScale;
            scaledTrain.Y = -25 + mapCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;
            if (showActiveTrainsRadio.Checked)
            {
                if (t is Simulation.AIs.AITrain && MapDataProvider.IsActiveTrain(t as Simulation.AIs.AITrain))
                    ShowTrainNameAndState(g, scaledTrain, t, trainName);
            }
            else
            {
                ShowTrainNameAndState(g, scaledTrain, t, trainName);
            }
        }

        private void ShowTrainNameAndState(Graphics g, PointF scaledItem, Train t, string trainName)
        {
            if (simulator.TimetableMode)
            {
                var tTTrain = t as Simulation.Timetables.TTTrain;
                if (tTTrain != null)
                {
                    // Remove name of timetable, e.g.: ":SCE"
                    var lastPos = trainName.LastIndexOf(":");
                    var shortName = (lastPos > 0) ? trainName.Substring(0, lastPos) : trainName;

                    if (MapDataProvider.IsActiveTrain(tTTrain))
                    {
                        if (showTrainStateCheckbox.Checked)
                        {
                            // 4:AI mode, 6:Mode, 7:Auth, 9:Signal, 12:Path
                            var status = tTTrain.GetStatus(Viewer.MilepostUnitsMetric);

                            // Add in fields 4 and 7
                            status = tTTrain.AddMovementState(status, Viewer.MilepostUnitsMetric);

                            var statuses = $"{status[4]} {status[6]} {status[7]} {status[9]}";

                            // Add path if it contains any deadlock information
                            if (MapDataProvider.ContainsDeadlockIndicators(status[12]))
                                statuses += status[12];

                            g.DrawString($"{shortName} {statuses}", trainFont, trainBrush, scaledItem);
                        }
                        else
                            g.DrawString(shortName, trainFont, trainBrush, scaledItem);
                    }
                    else
                        g.DrawString(shortName, trainFont, InactiveTrainBrush, scaledItem);
                }
            }
            else
                g.DrawString(trainName, trainFont, trainBrush, scaledItem);
        }

        private void ShowSwitches(Graphics g, float width)
        {
            if (!showSwitchesCheckbox.Checked)
                return;

            for (var i = 0; i < switches.Count; i++)
            {
                SwitchWidget sw = switches[i];

                var x = (sw.Location.X - subX) * xScale;
                var y = mapCanvas.Height - (sw.Location.Y - subY) * yScale;
                if (x < 0 || y < 0)
                    continue;

                var scaledItem = new PointF() { X = x, Y = y };

                if (sw.Item.TrJunctionNode.SelectedRoute == sw.main)
                    g.FillEllipse(new SolidBrush(Color.FromArgb(93, 64, 55)), DispatchViewer.GetRect(scaledItem, width));
                else
                    g.FillEllipse(new SolidBrush(Color.FromArgb(161, 136, 127)), DispatchViewer.GetRect(scaledItem, width));

                sw.Location2D.X = scaledItem.X; sw.Location2D.Y = scaledItem.Y;
                switchItemsDrawn.Add(sw);
            }
        }

        private void ShowSignals(Graphics g, PointF scaledB, float width)
        {
            if (!showSignalsCheckbox.Checked)
                return;

            foreach (var s in signals)
            {
                if (float.IsNaN(s.Location.X) || float.IsNaN(s.Location.Y))
                    continue;
                var x = (s.Location.X - subX) * xScale;
                var y = mapCanvas.Height - (s.Location.Y - subY) * yScale;
                if (x < 0 || y < 0)
                    continue;

                var scaledItem = new PointF() { X = x, Y = y };
                s.Location2D.X = scaledItem.X; s.Location2D.Y = scaledItem.Y;
                if (s.Signal.isSignalNormal())
                {
                    var color = new SolidBrush(Color.FromArgb(76, 175, 80));
                    var pen = greenPen;
                    if (s.IsProceed == 0)
                    {
                    }
                    else if (s.IsProceed == 1)
                    {
                        color = new SolidBrush(Color.FromArgb(255, 235, 59));
                        pen = orangePen;
                    }
                    else
                    {
                        color = new SolidBrush(Color.FromArgb(244, 67, 54));
                        pen = redPen;
                    }
                    g.FillEllipse(color, DispatchViewer.GetRect(scaledItem, width));
                    signalItemsDrawn.Add(s);
                    if (s.hasDir)
                    {
                        scaledB.X = (s.Dir.X - subX) * xScale; scaledB.Y = mapCanvas.Height - (s.Dir.Y - subY) * yScale;
                        g.DrawLine(pen, scaledItem, scaledB);
                    }
                    ShowSignalState(g, scaledItem, s);
                }
            }
        }

        private void ShowSignalState(Graphics g, PointF scaledItem, SignalWidget sw)
        {
            if (!showSignalStateCheckbox.Checked)
                return;

            var item = sw.Item as SignalItem;
            var trainNumber = sw.Signal?.enabledTrain?.Train?.Number;
            var trainString = (trainNumber == null) ? "" : $" train: {trainNumber}";
            var offset = 0;
            var position = scaledItem;
            foreach (var signalHead in sw.Signal.SignalHeads)
            {
                offset++;
                position.X += offset * 10;
                position.Y += offset * 15;
                var text = $"  {item?.SigObj} {signalHead.SignalTypeName} {signalHead.state} {trainString}";
                scaledItem.Y = MapDataProvider.GetUnusedYLocation(scaledItem.X, mapCanvas.Height - (sw.Location.Y - subY) * yScale, text);
                if (scaledItem.Y >= 0f) // -1 indicates no free slot to draw label
                    g.DrawString(text, SignalFont, SignalBrush, scaledItem);
            }

        }

        private void ShowSidingLabels(Graphics g)
        {
            if (!showSidingLabelsCheckbox.Checked)
                return;

            foreach (var s in sidings)
            {
                var scaledItem = new PointF();

                scaledItem.X = (s.Location.X - subX) * xScale;
                scaledItem.Y = MapDataProvider.GetUnusedYLocation(scaledItem.X, mapCanvas.Height - (s.Location.Y - subY) * yScale, s.Name);
                if (scaledItem.Y >= 0f) // -1 indicates no free slot to draw label
                    g.DrawString(s.Name, sidingFont, sidingBrush, scaledItem);
            }
        }

        private void ShowPlatformLabels(Graphics g)
        {
            var platformMarginPxX = 5;

            if (!showPlatformLabelsCheckbox.Checked)
                return;

            foreach (var p in platforms)
            {
                var scaledItem = new PointF();
                scaledItem.X = (p.Location.X - subX) * xScale + platformMarginPxX;
                var yPixels = mapCanvas.Height - (p.Location.Y - subY) * yScale;

                // If track is close to horizontal, then start label search 1 row down to minimise overwriting platform line.
                if (p.Extent1.X != p.Extent2.X
                    && Math.Abs((p.Extent1.Y - p.Extent2.Y) / (p.Extent1.X - p.Extent2.X)) < 0.1)
                    yPixels += DispatchViewer.spacing;

                scaledItem.Y = MapDataProvider.GetUnusedYLocation(scaledItem.X, mapCanvas.Height - (p.Location.Y - subY) * yScale, p.Name);
                if (scaledItem.Y >= 0f) // -1 indicates no free slot to draw label
                    g.DrawString(p.Name, PlatformFont, PlatformBrush, scaledItem);
            }
        }

        /// <summary>
        /// If the train is long enough then draw every car else just draw it as one or two blocks
        /// </summary>
        /// <param name="train"></param>
        /// <param name="minTrainLengthM"></param>
        /// <returns></returns>
        private bool IsDrawEveryCar(Train train, float minTrainLengthM)
        {
            float trainLengthM = 0f;
            foreach (var car in train.Cars)
            {
                trainLengthM += car.CarLengthM;
                if (trainLengthM > minTrainLengthM)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Indicates the location around which the image is zoomed.
        /// If user drags an item of interest into this target box and zooms in, the item will remain in view.
        /// </summary>
        /// <param name="g"></param>
        private void DrawZoomTarget(Graphics g)
        {
            if (!Dragging)
                return;

            const int size = 24;
            var top = mapCanvas.Height / 2 - size / 2;
            var left = mapCanvas.Width / 2 - size / 2;
            g.DrawRectangle(ZoomTargetPen, left, top, size, size);

        }













        private PointF DrawSiding(Graphics g, PointF scaledItem, SidingWidget s)
        {
            scaledItem.X = (s.Location.X - subX) * xScale;
            scaledItem.Y = DetermineSidingLocation(scaledItem.X, mapCanvas.Height - (s.Location.Y - subY) * yScale, s.Name);
            if (scaledItem.Y >= 0f) //if we need to draw the siding names
            {
                g.DrawString(s.Name, sidingFont, sidingBrush, scaledItem);
            }
            return scaledItem;
        }

        private PointF DrawPlatform(Graphics g, PointF scaledItem, PlatformWidget s)
        {
            scaledItem.X = (s.Location.X - subX) * xScale;
            scaledItem.Y = DetermineSidingLocation(scaledItem.X, mapCanvas.Height - (s.Location.Y - subY) * yScale, s.Name);
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
                            scaledA.Y = (float)(mapCanvas.Height - (previousLocation.TileZ * WorldLocation.TileSize + previousLocation.Location.Z - subY) * yScale);
                            scaledB.X = (float)((currentLocation.TileX * WorldLocation.TileSize + currentLocation.Location.X - subX) * xScale);
                            scaledB.Y = (float)(mapCanvas.Height - (currentPosition.TileZ * WorldLocation.TileSize + currentPosition.Location.Z - subY) * yScale); g.DrawLine(pathPen, scaledA, scaledB);
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

        void UITimer_Tick(object sender, EventArgs e)
        {
            if (Viewer.DebugViewerBetaEnabled == false) // Ctrl+9 sets this true to initialise the window and make it visible
            {
                Visible = false;
                return;
            }
            Visible = true;

            if (Program.Simulator.GameTime - lastUpdateTime < 1)
                return;

            lastUpdateTime = Program.Simulator.GameTime;

            GenerateView();
        }

        private void allowJoiningCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            MPManager.Instance().AllowNewPlayer = allowJoiningCheckbox.Checked;
        }

        private void drawPathCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            DrawPath = drawPathCheckbox.Checked;
        }

        private void mapResolutionUpDown_ValueChanged(object sender, EventArgs e)
        {
            // Center point of the map viewport before the change in resolution
            PointF center = new PointF(ViewWindow.X + ViewWindow.Width / 2f, ViewWindow.Y + ViewWindow.Height / 2f);

            float newSizeH = (float)mapResolutionUpDown.Value;
            float verticalByHorizontal = ViewWindow.Height / ViewWindow.Width;
            float newSizeV = newSizeH * verticalByHorizontal;

            ViewWindow = new RectangleF(center.X - newSizeH / 2f, center.Y - newSizeV / 2f, newSizeH, newSizeV);

            GenerateView();
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            decimal tempValue = mapResolutionUpDown.Value;
            if (e.Delta < 0) tempValue /= 0.95m;
            else if (e.Delta > 0) tempValue *= 0.95m;
            else return;

            if (tempValue < mapResolutionUpDown.Minimum) tempValue = mapResolutionUpDown.Minimum;
            if (tempValue > mapResolutionUpDown.Maximum) tempValue = mapResolutionUpDown.Maximum;
            mapResolutionUpDown.Value = tempValue;
        }

        private bool Zooming;
        private bool LeftClick;
        private bool RightClick;

        private void mapCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) LeftClick = true;
            if (e.Button == MouseButtons.Right) RightClick = true;

            if (LeftClick == true && RightClick == false)
            {
                if (Dragging == false)
                {
                    Dragging = true;
                    Cursor.Current = Cursors.NoMove2D;
                }
            }
            else if (LeftClick == true && RightClick == true)
            {
                if (Zooming == false) Zooming = true;
            }
            LastCursorPosition.X = e.X;
            LastCursorPosition.Y = e.Y;
            MPManager.Instance().ComposingText = false;
            /*lblInstruction1.Visible = true;
            lblInstruction2.Visible = true;
            lblInstruction3.Visible = true;
            lblInstruction4.Visible = true;*/
        }

        private void mapCanvas_MouseUp(object sender, MouseEventArgs e)
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
                CanvasMoveAndZoomInOut(e.X, e.Y, 1200);
            }
            else if ((ModifierKeys & Keys.Alt) == Keys.Alt)
            {
                CanvasMoveAndZoomInOut(e.X, e.Y, 30000);
            }
            else if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                CanvasMoveAndZoomInOut(e.X, e.Y, mapResolutionUpDown.Maximum);
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
            /*lblInstruction1.Visible = false;
            lblInstruction2.Visible = false;
            lblInstruction3.Visible = false;
            lblInstruction4.Visible = false;*/
        }

        private void UnHandleItemPick()
        {
            setSignalMenu.Visible = false;
            setSwitchMenu.Visible = false;
        }

        private void HandlePickedSignal()
        {
            if (MPManager.IsClient() && !MPManager.Instance().AmAider) // normal client not server or aider
                return;
            setSwitchMenu.Visible = false;
            if (signalPickedItem == null) return;

            allowCallOnToolStripMenuItem.Enabled = false;
            if (signalPickedItem.Signal.enabledTrain != null && signalPickedItem.Signal.CallOnEnabled && !signalPickedItem.Signal.CallOnManuallyAllowed)
                allowCallOnToolStripMenuItem.Enabled = true;

            setSignalMenu.Show(Cursor.Position);
            setSignalMenu.Enabled = true;
            setSignalMenu.Focus();
            setSignalMenu.Visible = true;
            return;
        }

        private void HandlePickedSwitch()
        {
            if (MPManager.IsClient() && !MPManager.Instance().AmAider)
                return;//normal client not server

            setSignalMenu.Visible = false;
            if (switchPickedItem == null) return;
            setSwitchMenu.Show(Cursor.Position);
            setSwitchMenu.Enabled = true;
            setSwitchMenu.Focus();
            setSwitchMenu.Visible = true;
            return;
        }

        private void setSignalMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (signalPickedItem == null)
            {
                UnHandleItemPick();
                return;
            }

            var signal = signalPickedItem.Signal;
            var type = e.ClickedItem.Tag.ToString();

            string[] signalAspects = { "system", "stop", "approach", "proceed" };
            int numericSignalAspect = Array.IndexOf(signalAspects, "stop");

            if (MPManager.Instance().AmAider)
            {
                MPManager.Notify(new MSGSignalChange(signal, numericSignalAspect).ToString());
                UnHandleItemPick();
                return;
            }

            switch (type)
            {
                case "system":
                    signal.ClearHoldSignalDispatcher();
                    break;

                case "stop":
                    signal.RequestHoldSignalDispatcher(true);
                    break;

                case "approach":
                    signal.RequestApproachAspect();
                    break;

                case "proceed":
                    signal.RequestLeastRestrictiveAspect();
                    break;

                case "callOn":
                    signal.SetManualCallOn(true);
                    break;
            }

            UnHandleItemPick();
        }

        private void setSwitchMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (switchPickedItem == null)
            {
                UnHandleItemPick(); return;
            }
            var sw = switchPickedItem.Item.TrJunctionNode;
            var type = e.ClickedItem.Tag.ToString();

            // Aider can send message to the server for a switch
            if (MPManager.IsMultiPlayer() && MPManager.Instance().AmAider)
            {
                var nextSwitchTrack = sw;
                var Selected = 0;
                switch (type)
                {
                    case "mainRoute":
                        Selected = (int)switchPickedItem.main;
                        break;
                    case "sideRoute":
                        Selected = 1 - (int)switchPickedItem.main;
                        break;
                }
                // Aider selects and throws the switch, but need to confirm by the dispatcher
                MPManager.Notify(new MSGSwitch(MPManager.GetUserName(),
                    nextSwitchTrack.TN.UiD.WorldTileX, nextSwitchTrack.TN.UiD.WorldTileZ, nextSwitchTrack.TN.UiD.WorldId, Selected, true).ToString());
                Program.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switching Request Sent to the Server"));

            }
            else // Server throws the switch immediately
            {
                switch (type)
                {
                    case "mainRoute":
                        Program.Simulator.Signals.RequestSetSwitch(sw.TN, (int)switchPickedItem.main);
                        //sw.SelectedRoute = (int)switchPickedItem.main;
                        break;
                    case "sideRoute":
                        Program.Simulator.Signals.RequestSetSwitch(sw.TN, 1 - (int)switchPickedItem.main);
                        //sw.SelectedRoute = 1 - (int)switchPickedItem.main;
                        break;
                }
            }
            UnHandleItemPick();
        }

        private ItemWidget findItemFromMouse(int x, int y, int range)
        {
            if (range < 5) range = 5;
            double closest = float.NaN;
            ItemWidget closestItem = null;
            if (allowThrowingSwitchesCheckbox.Checked == true)
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
            if (allowChangingSignalsCheckbox.Checked == true)
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
                tY = mapCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;
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
                //AvatarView.SelectedItems.Clear();
                return new TrainWidget(PickedTrain);
            }
            return null;
        }

        private void mapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Dragging && !Zooming)
            {
                int diffX = LastCursorPosition.X - e.X;
                int diffY = LastCursorPosition.Y - e.Y;

                ViewWindow.Offset(diffX * ScrollSpeedX / 10, -diffY * ScrollSpeedY / 10);
                GenerateView();
            }
            else if (Zooming)
            {
                decimal tempValue = mapResolutionUpDown.Value;
                if (LastCursorPosition.Y - e.Y < 0) tempValue /= 0.95m;
                else if (LastCursorPosition.Y - e.Y > 0) tempValue *= 0.95m;

                if (tempValue < mapResolutionUpDown.Minimum) tempValue = mapResolutionUpDown.Minimum;
                if (tempValue > mapResolutionUpDown.Maximum) tempValue = mapResolutionUpDown.Maximum;
                mapResolutionUpDown.Value = tempValue;
                GenerateView();
            }
            LastCursorPosition.X = e.X;
            LastCursorPosition.Y = e.Y;
        }

        private void CanvasMoveAndZoomInOut(int x, int y, decimal scale)
        {
            int diffX = x - mapCanvas.Width / 2;
            int diffY = y - mapCanvas.Height / 2;
            ViewWindow.Offset(diffX / xScale, -diffY / yScale);
            if (scale < mapResolutionUpDown.Minimum) scale = mapResolutionUpDown.Minimum;
            if (scale > mapResolutionUpDown.Maximum) scale = mapResolutionUpDown.Maximum;
            mapResolutionUpDown.Value = scale;
            GenerateView();
        }

        private void playerRoleLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://open-rails.readthedocs.io/en/latest/multiplayer.html#in-game-controls");
        }

        private void mapCustomizationButton_Click(object sender, EventArgs e)
        {
            MapCustomizationVisible = !MapCustomizationVisible;
            mapCustomizationPanel.Visible = MapCustomizationVisible;

            if (MapCustomizationVisible == true)
            {
                mapCustomizationButton.BackColor = Color.FromArgb(214, 234, 248);
                mapCustomizationButton.ForeColor = Color.FromArgb(40, 116, 166);
            }
            else
            {
                mapCustomizationButton.BackColor = SystemColors.Control;
                mapCustomizationButton.ForeColor = SystemColors.ControlText;
            }
        }

        private void playersView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var focusedItem = playersView.FocusedItem;
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                {
                    playerActionsMenu.Show(Cursor.Position);
                }
            }
        }

        public bool ClickedTrain;
        private void seeTrainInGameButton_Click(object sender, EventArgs e)
        {
            if (PickedTrain != null) ClickedTrain = true;
            else ClickedTrain = false;
        }

        private void centerOnMyTrainButton_Click(object sender, EventArgs e)
        {
            followTrain = false;
            firstShow = true;
            GenerateView();
        }

        private void penaltyCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            MPManager.Instance().CheckSpad = penaltyCheckbox.Checked;
            if (penaltyCheckbox.Checked == false) { MPManager.BroadCast(new MSGMessage("All", "OverSpeedOK", "OK to go overspeed and pass stop light").ToString()); }
            else { MPManager.BroadCast(new MSGMessage("All", "NoOverSpeed", "Penalty for overspeed and passing stop light").ToString()); }
        }



        private void DispatchViewerBeta_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Prevent the window from closing; instead, hide it
            e.Cancel = true;
            Viewer.DebugViewerBetaEnabled = false;
        }


    }

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
    }

}
