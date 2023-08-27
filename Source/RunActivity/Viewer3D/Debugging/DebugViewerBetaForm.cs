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
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using Orts.Simulation.Timetables;
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
        private readonly MapDataProvider MapDataProvider;
        private readonly MapThemeProvider MapThemeProvider;
        private string ThemeName = "light";
        private ThemeStyle Theme;
        /// <summary>
        /// Used to periodically check if we should shift the view when the
        /// user is holding down a "shift view" button.
        /// </summary>
        private readonly Timer UITimer;
        public Viewer Viewer;

        /// <summary>
        /// True when the user is dragging the route view
        /// </summary>
        public bool Dragging;
        private WorldPosition worldPos;
        public float xScale = 1; // pixels / metre
        public float yScale = 1; // pixels / metre

        readonly string name = "";

        public List<SwitchWidget> switchItemsDrawn;
        public List<SignalWidget> signalItemsDrawn;
        public SwitchWidget switchPickedItem;
        public SignalWidget signalPickedItem;
        public bool switchPickedItemHandled;
        public double switchPickedTime;
        public bool signalPickedItemHandled;
        public double signalPickedTime;
        public bool DrawPath = true; //draw train path
        readonly TrackNode[] nodes;

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

        public Font trainFont = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
        public Font sidingFont = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
        public Font PlatformFont = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
        public Font SignalFont = new Font("Segoe UI Semibold", 10, FontStyle.Regular);
        private readonly SolidBrush trainBrush = new SolidBrush(Color.Red);
        public SolidBrush sidingBrush = new SolidBrush(Color.Blue);
        public SolidBrush PlatformBrush = new SolidBrush(Color.DarkBlue);
        public SolidBrush SignalBrush = new SolidBrush(Color.DarkRed);
        public SolidBrush InactiveTrainBrush = new SolidBrush(Color.DarkRed);
        private Color MapCanvasColor = Color.White;

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
            MapThemeProvider = new MapThemeProvider();
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
            MapDataProvider.SetControls();
            MapThemeProvider.InitializeThemes();
            Theme = MapThemeProvider.GetTheme(ThemeName);

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
            // Take up to next 500
            maxsize = (int)((maxsize / 100) + 1) * 500;
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
            // When minimizing the window, `mapCanvas.Width` gets reported as 0
            // This crashes `System.Drawing.dll`, hence the check below
            if (mapCanvas.Width <= 0 || mapCanvas.Height <= 0) return;

            mapCanvas.Image?.Dispose();
            mapCanvas.Image = new Bitmap(mapCanvas.Width, mapCanvas.Height);
        }
        #endregion

        #region playersList
        readonly List<string> PlayersList = new List<string>();

        public void AddPlayer(string name)
        {
            PlayersList.Add(name);
        }

        // TODO: Colour code players based on their roles
        // TODO: FUNCTION NOT WORKING CORRECTLY
        public void CheckPlayers()
        {
            if (!MPManager.IsMultiPlayer() || MPManager.OnlineTrains == null || MPManager.OnlineTrains.Players == null) return;
            var players = MPManager.OnlineTrains.Players;
            var username = MPManager.GetUserName();
            players = players.Concat(MPManager.Instance().lostPlayer).ToDictionary(x => x.Key, x => x.Value);

            bool PlayersListChanged = false;

            // Add myself
            if (!PlayersList.Contains(username))
            {
                AddPlayer(username);
                PlayersListChanged = true;
            }

            foreach (var p in players)
            {
                if (PlayersList.Contains(p.Key)) continue;
                AddPlayer(p.Key);
                PlayersListChanged = true;
            }

            // Remove players from `PlayersList` if they are no longer on the server
            foreach (var p in PlayersList.ToList()) // https://stackoverflow.com/a/604843/
            {
                if (players.ContainsKey(p) || p == username) continue;
                PlayersList.Remove(p);
                PlayersListChanged = true;
            }

            // If `PlayersList` hasn't changed since we've last checked, we should not clear/update
            // `playersView` to avoid flickering
            if (!PlayersListChanged) return;

            playersView.Items.Clear();
            Console.Beep();
            foreach (var p in PlayersList)
            {
                if (p == username)
                    playersView.Items.Add(p);

                if (MPManager.Instance().aiderList.Contains(p))
                {
                    playersView.Items.Add(p + " (Helper)");
                }
                else if (MPManager.Instance().lostPlayer.ContainsKey(p))
                {
                    playersView.Items.Add(p + " (Disconnected)");
                }
                else
                {
                    playersView.Items.Add(p);
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

            if (mapCanvas.Image == null || firstShow) InitializeImage();

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
                    var ploc = new PointF((pos.TileX * 2048) + pos.Location.X, (pos.TileZ * 2048) + pos.Location.Z);
                    ViewWindow.X = ploc.X - minX - (ViewWindow.Width / 2); ViewWindow.Y = ploc.Y - minY - (ViewWindow.Width / 2);
                    firstShow = false;
                }
            }

            CheckPlayers();

            using (Graphics g = Graphics.FromImage(mapCanvas.Image))
            {
                // Optional anti-aliasing
                if (useAntiAliasingCheckbox.Checked == true)
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                subX = minX + ViewWindow.X; subY = minY + ViewWindow.Y;
                g.Clear(MapCanvasColor);

                xScale = mapCanvas.Width / ViewWindow.Width;
                yScale = mapCanvas.Height / ViewWindow.Height;
                xScale = yScale = Math.Max(xScale, yScale); // Make X and Y scales the same to maintain correct angles

                // Set the default pen to represent 1 meter
                var scale = (float)Math.Round(xScale);  // Round to nearest pixels/meter
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
                var scaledA = new PointF((p.Extent1.X - subX) * xScale, mapCanvas.Height - ((p.Extent1.Y - subY) * yScale));
                var scaledB = new PointF((p.Extent2.X - subX) * xScale, mapCanvas.Height - ((p.Extent2.Y - subY) * yScale));

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
                scaledA.X = ((line.A.TileX * 2048) - subX + (float)line.A.X) * xScale;
                scaledA.Y = mapCanvas.Height - (((line.A.TileZ * 2048) - subY + (float)line.A.Z) * yScale);
                scaledB.X = ((line.B.TileX * 2048) - subX + (float)line.B.X) * xScale;
                scaledB.Y = mapCanvas.Height - (((line.B.TileZ * 2048) - subY + (float)line.B.Z) * yScale);

                if ((scaledA.X < 0 && scaledB.X < 0)
                    || (scaledA.Y < 0 && scaledB.Y < 0))
                    continue;

                if (line.isCurved == true)
                {
                    scaledC.X = ((float)line.C.X - subX) * xScale; scaledC.Y = mapCanvas.Height - (((float)line.C.Z - subY) * yScale);
                    points[0] = scaledA; points[1] = scaledC; points[2] = scaledB;
                    g.DrawCurve(TrackPen, points);
                }
                else g.DrawLine(TrackPen, scaledA, scaledB);
            }
        }

        private void DrawTrains(Graphics g, PointF scaledA, PointF scaledB)
        {
            var margin = 30 * xScale; // Margins to determine if we want to draw a train
            var margin2 = 5000 * xScale;

            // Variable for drawing train path
            var mDist = 5000f; var pDist = 50; // Segment length when drawing path

            selectedTrainList.Clear();

            if (simulator.TimetableMode)
            {
                // Add the player's train...
                if (simulator.PlayerLocomotive.Train is AITrain)
                    selectedTrainList.Add(simulator.PlayerLocomotive.Train as AITrain);

                // ...then all the AI trains, including static consists.
                foreach (AITrain train in simulator.AI.AITrains)
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
                    if (train.TrainType == Train.TRAINTYPE.AI || train.TrainType == Train.TRAINTYPE.STATIC)
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
                float x = ((loc.TileX * 2048) + loc.Location.X - subX) * xScale;
                float y = mapCanvas.Height - (((loc.TileZ * 2048) + loc.Location.Z - subY) * yScale);

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
                var scaledTrain = new PointF
                {
                    X = ((worldPos.TileX * 2048) - subX + worldPos.Location.X) * xScale,
                    Y = -25 + mapCanvas.Height - (((worldPos.TileZ * 2048) - subY + worldPos.Location.Z) * yScale)
                };
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
                    t.Move(dist + (car.CarLengthM / 2)); // Move along from centre of car to front of car
                    x = ((t.TileX * 2048) + t.Location.X - subX) * xScale;
                    y = mapCanvas.Height - (((t.TileZ * 2048) + t.Location.Z - subY) * yScale);

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
                        t.Move(dist + (car.CarLengthM / 2));
                        x = ((t.TileX * 2048) + t.Location.X - subX) * xScale;
                        y = mapCanvas.Height - (((t.TileZ * 2048) + t.Location.Z - subY) * yScale);

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
                        t.Move(dist + (train.Cars.First().CarLengthM / 2) - (minTrainPx / xScale / 2));
                        x = ((t.TileX * 2048) + t.Location.X - subX) * xScale;
                        y = mapCanvas.Height - (((t.TileZ * 2048) + t.Location.Z - subY) * yScale);
                        if (x < -margin || y < -margin)
                            return;
                        t.Move(-minTrainPx / xScale / 2);
                    }
                    scaledTrain.X = x; scaledTrain.Y = y;
                }
                x = ((t.TileX * 2048) + t.Location.X - subX) * xScale;
                y = mapCanvas.Height - (((t.TileZ * 2048) + t.Location.Z - subY) * yScale);

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
            if (MapDataProvider.IsActiveTrain(t as AITrain))
            {
                trainPen.Color = car is MSTSLocomotive
                    ? (car == locoCar) ? Color.FromArgb(204, 170, 0) : Color.FromArgb(153, 128, 0)
                    : Color.FromArgb(0, 204, 0);
            }
            else
            {
                trainPen.Color = car is MSTSLocomotive ? Color.FromArgb(153, 128, 0) : Color.FromArgb(0, 153, 0);
            }

            if (t.TrainType == Train.TRAINTYPE.STATIC || (t.TrainType == Train.TRAINTYPE.AI && t.GetAIMovementState() == AITrain.AI_MOVEMENT_STATE.AI_STATIC))
            {
                trainPen.Color = Color.FromArgb(83, 237, 214);
            }

            // Draw player train with loco in red
            if (t.TrainType == Train.TRAINTYPE.PLAYER && car == locoCar)
                trainPen.Color = Color.Red;
        }

        private void DrawTrainLabels(Graphics g, Train t, string trainName, TrainCar firstCar, PointF scaledTrain)
        {
            WorldPosition worldPos = firstCar.WorldPosition;
            scaledTrain.X = ((worldPos.TileX * 2048) - subX + worldPos.Location.X) * xScale;
            scaledTrain.Y = -25 + mapCanvas.Height - (((worldPos.TileZ * 2048) - subY + worldPos.Location.Z) * yScale);
            if (showActiveTrainsRadio.Checked)
            {
                if (t is AITrain && MapDataProvider.IsActiveTrain(t as AITrain))
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
                if (t is TTTrain tTTrain)
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
                var y = mapCanvas.Height - ((sw.Location.Y - subY) * yScale);
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
                var y = mapCanvas.Height - ((s.Location.Y - subY) * yScale);
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
                        scaledB.X = (s.Dir.X - subX) * xScale; scaledB.Y = mapCanvas.Height - ((s.Dir.Y - subY) * yScale);
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
                scaledItem.Y = MapDataProvider.GetUnusedYLocation(scaledItem.X, mapCanvas.Height - ((sw.Location.Y - subY) * yScale), text);
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
                scaledItem.Y = MapDataProvider.GetUnusedYLocation(scaledItem.X, mapCanvas.Height - ((s.Location.Y - subY) * yScale), s.Name);
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
                scaledItem.X = ((p.Location.X - subX) * xScale) + platformMarginPxX;
                var yPixels = mapCanvas.Height - ((p.Location.Y - subY) * yScale);

                // If track is close to horizontal, then start label search 1 row down to minimise overwriting platform line.
                if (p.Extent1.X != p.Extent2.X
                    && Math.Abs((p.Extent1.Y - p.Extent2.Y) / (p.Extent1.X - p.Extent2.X)) < 0.1)
                    yPixels += DispatchViewer.spacing;

                scaledItem.Y = MapDataProvider.GetUnusedYLocation(scaledItem.X, mapCanvas.Height - ((p.Location.Y - subY) * yScale), p.Name);
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
            var top = (mapCanvas.Height / 2) - (size / 2);
            var left = (mapCanvas.Width / 2) - (size / 2);
            g.DrawRectangle(ZoomTargetPen, left, top, size, size);

        }

        public Vector2[][] alignedTextY;
        public int[] alignedTextNum;
        public const int spacing = 12;

        const float SignalErrorDistance = 100;
        const float SignalWarningDistance = 500;
        const float DisplayDistance = 1000;
        const float DisplaySegmentLength = 10;
        const float MaximumSectionDistance = 10000;

        readonly Dictionary<int, SignallingDebugWindow.TrackSectionCacheEntry> Cache = new Dictionary<int, SignallingDebugWindow.TrackSectionCacheEntry>();
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

                    if (obj is SignallingDebugWindow.TrackSectionSwitch switchObj)
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
                            scaledA.X = (float)(((previousLocation.TileX * WorldLocation.TileSize) + previousLocation.Location.X - subX) * xScale);
                            scaledA.Y = (float)(mapCanvas.Height - (((previousLocation.TileZ * WorldLocation.TileSize) + previousLocation.Location.Z - subY) * yScale));
                            scaledB.X = (float)(((currentLocation.TileX * WorldLocation.TileSize) + currentLocation.Location.X - subX) * xScale);
                            scaledB.Y = (float)(mapCanvas.Height - (((currentPosition.TileZ * WorldLocation.TileSize) + currentPosition.Location.Z - subY) * yScale)); g.DrawLine(pathPen, scaledA, scaledB);
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

                    if (obj is SignallingDebugWindow.TrackSectionSwitch switchObj)
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

        #region themes
        private void ApplyThemeRecursively(System.Windows.Forms.Control parent)
        {
            foreach (System.Windows.Forms.Control c in parent.Controls)
            {
                if (c is Button button && c?.Tag?.ToString() != "mapCustomization")
                {
                    Button b = button;
                    b.BackColor = Theme.BackColor;
                    b.ForeColor = Theme.ForeColor;
                    b.FlatStyle = Theme.FlatStyle;
                }
                else if (c is GroupBox || c is Panel)
                {
                    c.BackColor = Theme.PanelBackColor;
                    c.ForeColor = Theme.ForeColor;
                }
                else
                {
                    c.BackColor = Theme.PanelBackColor;
                    c.ForeColor = Theme.ForeColor;
                }

                ApplyThemeRecursively(c);
            }
        }
        #endregion

        /// <summary>
        /// Generates a rectangle representing a dot being drawn.
        /// </summary>
        /// <param name="p">Center point of the dot, in pixels.</param>
        /// <param name="size">Size of the dot's diameter, in pixels</param>
        /// <returns></returns>
        public static RectangleF GetRect(PointF p, float size)
        {
            return new RectangleF(p.X - (size / 2f), p.Y - (size / 2f), size, size);
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

                tempX1 = (A.TileX * 2048) + A.X; tempX2 = (B.TileX * 2048) + B.X;
                tempZ1 = (A.TileZ * 2048) + A.Z; tempZ2 = (B.TileZ * 2048) + B.Z;
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
            PointF center = new PointF(ViewWindow.X + (ViewWindow.Width / 2f), ViewWindow.Y + (ViewWindow.Height / 2f));

            float newSizeH = (float)mapResolutionUpDown.Value;
            float verticalByHorizontal = ViewWindow.Height / ViewWindow.Width;
            float newSizeV = newSizeH * verticalByHorizontal;

            ViewWindow = new RectangleF(center.X - (newSizeH / 2f), center.Y - (newSizeV / 2f), newSizeH, newSizeV);

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
                        if (temp is SwitchWidget widget)
                        {
                            switchPickedItem = widget;
                            signalPickedItem = null;
                            HandlePickedSwitch();
                        }
                        if (temp is SignalWidget widget1)
                        {
                            signalPickedItem = widget1;
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
                tX = ((worldPos.TileX * 2048) - subX + worldPos.Location.X) * xScale;
                tY = mapCanvas.Height - (((worldPos.TileZ * 2048) - subY + worldPos.Location.Z) * yScale);
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
            int diffX = x - (mapCanvas.Width / 2);
            int diffY = y - (mapCanvas.Height / 2);
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
            ClickedTrain = PickedTrain != null;
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

        private void DispatchViewerBeta_Resize(object sender, EventArgs e)
        {
            InitializeImage();
        }

        private void rotateThemesButton_Click(object sender, EventArgs e)
        {
            // Cycles through the array of available themes
            string[] themes = MapThemeProvider.GetThemes();
            int i = Array.IndexOf(themes, ThemeName);
            ThemeName = i >= 0 && i < themes.Length - 1 ? themes[i + 1] : themes[0];

            Theme = MapThemeProvider.GetTheme(ThemeName);

            ApplyThemeRecursively(this);
            MapCanvasColor = Theme.MapCanvasColor;
            TrackPen.Color = Theme.TrackColor;
            InitializeImage();
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
            Location.X = (item.TileX * 2048) + item.X;
            Location.Y = (item.TileZ * 2048) + item.Z;
            var node = Program.Simulator.TDB.TrackDB.TrackNodes?[signal.trackNode];
            Vector2 v2;
            if (node?.TrVectorNode != null)
            {
                var ts = node.TrVectorNode.TrVectorSections?.FirstOrDefault();
                if (ts == null)
                    return;
                v2 = new Vector2((ts.TileX * 2048) + ts.X, (ts.TileZ * 2048) + ts.Z);
            }
            else if (node?.TrJunctionNode != null)
            {
                var ts = node?.UiD;
                if (ts == null)
                    return;
                v2 = new Vector2((ts.TileX * 2048) + ts.X, (ts.TileZ * 2048) + ts.Z);
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

            main = TS != null ? TS.MainRoute : 0;

            Location.X = (Item.UiD.TileX * 2048) + Item.UiD.X; Location.Y = (Item.UiD.TileZ * 2048) + Item.UiD.Z;
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

            Location.X = (Item.UiD.TileX * 2048) + Item.UiD.X; Location.Y = (Item.UiD.TileZ * 2048) + Item.UiD.Z;
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
                    Vector3 v = new Vector3((float)(((B.TileX - A.TileX) * 2048) + B.X - A.X), 0, (float)(((B.TileZ - A.TileZ) * 2048) + B.Z - A.Z));
                    isCurved = true;
                    Vector3 v2 = Vector3.Cross(Vector3.Up, v); v2.Normalize();
                    v /= 2; v.X += (A.TileX * 2048) + (float)A.X; v.Z += (A.TileZ * 2048) + (float)A.Z;
                    v = ts.SectionCurve.Angle > 0 ? (v2 * -diff) + v : (v2 * diff) + v;
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
            Location = new PointF((item.TileX * 2048) + item.X, (item.TileZ * 2048) + item.Z);
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
            Location = new PointF((item.TileX * 2048) + item.X, (item.TileZ * 2048) + item.Z);
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

        public static double DistanceSqr(dVector v1, dVector v2)
        {
            return Math.Pow(((v1.TileX - v2.TileX) * 2048) + v1.X - v2.X, 2)
                + Math.Pow(((v1.TileZ - v2.TileZ) * 2048) + v1.Z - v2.Z, 2);
        }
    }
}
