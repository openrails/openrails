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


using ActivityEditor.Activity;
using LibAE;
using LibAE.Formats;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.Parsers.Msts;
using ORTS;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using Color = System.Drawing.Color;

namespace ActivityEditor.Engine
{
    public partial class Viewer2D : Form
    {
        public TypeEditor ViewerMode;
        public string ActivityPath;
        Pen redPen = new Pen(Color.Red);
        Pen greenPen = new Pen(Color.Green);
        Pen orangePen = new Pen(Color.Orange);
        Pen darkGrayPen = new Pen(Color.DarkGray);
        Pen bluePen = new Pen(Color.Blue);
        Icon StationIcon;
        Icon TagIcon;
        Icon SignalIco;
        Icon ShuntIco;
        Icon OtherSigIco;
        Icon RepeatIco;
        Icon SpeedIco;
        Icon StationConnector;
        Image Ruler;
        private Font sidingFont;
        private Font stationFont;
        private SolidBrush sidingBrush;

        bool loaded = false;
        PointF lastContextMenuPos;

        public GlobalItem itemToUpdate = null;
        public GlobalItem itemToEdit = null;
        private StationItem stationItem = null;
        public ActEditor actParent;
        public AEConfig aeConfig;
        public PseudoSim Simulator;
        public MSTSItems aeItems { get { return Simulator.mstsItems; } protected set { } }
        Random rnd = new Random();

        private int IM_Width = 720;
        private int IM_Height = 720;
        public AreaRoute areaRoute { get { return Simulator.areaRoute; } protected set { } }

        //  To define the biggest draughtboard for the route
        List<TilesInfo> tilesList = new List<TilesInfo>();
        float sizeX, sizeZ;

        private bool Dragging = false;
        private bool Zooming = false;
        float xScale = 1;
        float yScale = 1;
        public ToolClicks ToolClicked = ToolClicks.NO_TOOL;
        public Image ToolToUse = null;
        public Cursor CursorToUse = Cursors.Default;
        public Cursor reduit;

        private float usedScale = 0;
        private float previousScale = 0;
        private float currentRules = 0;
        private float origRules = 0;
        public double snapSize { get; set; }

        //  Keep the distance between the map origin and the visible section
        private PointF RouteScreenOrig = new PointF(0,0);
        private SizeF Distant2Origin = new SizeF(0, 0);
        //  Son équivalent en int pour le forms
        private System.Drawing.SizeF StartDrawing = new System.Drawing.SizeF(0, 0);
        private SizeF correctY = new SizeF (0,0);

        private Size routePicture = new Size(0, 0);

        private bool LeftClick = false;
        private bool RightClick = false;

        //  Represent the size of the visible part of the route 
        private RectangleF ViewWindow;
        //  LastCursor is used to calculate the current motion when dragging
        private System.Drawing.Point LastCursorPosition = new System.Drawing.Point();
        //  refZoomPoint mark the reference position for the zoom, stored as MSTS coord.
        private System.Drawing.Point refZoomPoint = new System.Drawing.Point(0, 0);
        private MSTSCoord coordZoomPoint;

        private System.Drawing.Point CurrentMousePosition = new System.Drawing.Point(0, 0);

        public Timer ActivityTimer;

        //  For Debugging

#region InitViewer

        /// <summary>
        /// Initialise a new instance of the Viewer for ROUTE Configuration,
        /// with the parent windows and the ComponentItem of the route to be used
        /// </summary>
        /// <param name="parent">The parent ActEditor</param>
        /// <param name="routePath">The Route ComponentItem where to get the information</param>
        /// <returns>Nothing</returns>
        public Viewer2D(ActEditor parent, string routePath)
        {
            ViewerMode = TypeEditor.ROUTECONFIG;
            Program.actEditor.DisplayStatusMessage("Load Route for update: please wait");
            loadIcon();
            ActivityPath = routePath;

            //  Simulator retains all datas from MSTS, Route Metadata and Activity.
            Simulator = new PseudoSim(Program.aePreference.settings);
            Simulator.Start();

            //  Now, load all MSTS config, all OR config and Synchronize both
            Simulator.LoadRoute(routePath, ViewerMode);

            MdiParent = parent;
            actParent = parent;

            //  Load the right side panel
            aeConfig = new AEConfig(TypeEditor.ROUTECONFIG, actParent, this);
            aeConfig.LoadPanels("Info");
            aeConfig.LoadRoute();

            InitializeComponent();
            this.Text = "Route MetaData";
            sidingFont = new Font("Arial", 8, FontStyle.Regular);
            stationFont = new Font("Arial", 12, FontStyle.Regular);
            sidingBrush = new SolidBrush(Color.Black);

            SetStyle(ControlStyles.ResizeRedraw, true);
            // initialise the timer used to handle user input
            ActivityTimer = new Timer();
            ActivityTimer.Interval = 50;
            ActivityTimer.Tick += new System.EventHandler(ActivityTimer_Tick);
            ActivityTimer.Start();
            InitData();
            actParent.SelectTools(ViewerMode);
        }

        /// <summary>
        /// Initialise a new instance of the Viewer for ACTIVITY Edition,
        /// with the parent windows and the ActivityInfo object ready to use
        /// </summary>
        /// <param name="parent">The parent ActEditor</param>
        /// <param name="activityInfo">The pre-loaded activity info object</param>
        /// <returns>Nothing</returns>
        public Viewer2D(ActEditor parent, ActivityInfo activityInfo)
        {
            ViewerMode = TypeEditor.ACTIVITY;
            Program.actEditor.DisplayStatusMessage("Load Route and Activity for update: please wait");
            loadIcon();
            MdiParent = parent;
            actParent = parent;

            Simulator = new PseudoSim(Program.aePreference.settings);
            Simulator.Start();
            Simulator.LoadRoute(activityInfo.RoutePath, ViewerMode);

            aeConfig = new AEConfig(TypeEditor.ACTIVITY, actParent, this);
            aeConfig.LoadRoute();
            aeConfig.LoadActivity(activityInfo);
            aeConfig.LoadPanels("Info");

            InitializeComponent();
            sidingFont = new Font("Arial", 8, FontStyle.Regular);
            stationFont = new Font("Arial", 12, FontStyle.Regular);
            sidingBrush = new SolidBrush(Color.Black);

            SetStyle(ControlStyles.ResizeRedraw, true);
            // initialise the timer used to handle user input
            ActivityTimer = new Timer();
            ActivityTimer.Interval = 50;
            ActivityTimer.Tick += new System.EventHandler(ActivityTimer_Tick);
            ActivityTimer.Start();
            InitData();
            actParent.SelectTools(ViewerMode);
        }
#endregion
#region initData

        private void InitData()
        {
            if (!loaded)
            {
                // do this only once
                loaded = true;
                //trackSections.DataSource = new List<InterlockingTrack>(simulator.InterlockingSystem.Tracks.Values).ToArray();
            }
            Program.actEditor.DisplayStatusMessage("Init data for display...");
            //aeConfig.aeRouteConfig.InitORData(Simulator);
            tilesList = areaRoute.tilesList;
            

            sizeX = (Math.Abs(areaRoute.tileMaxX - areaRoute.tileMinX) + 1) * 2048f;
            sizeZ = (Math.Abs(areaRoute.tileMaxZ - areaRoute.tileMinZ) + 1) * 2048f;

            ViewWindow = new RectangleF(0, 0, sizeX, sizeZ);
            routePicture = routeDrawing.Size;
            xScale = routeDrawing.Width / ViewWindow.Width;
            yScale = routeDrawing.Height / ViewWindow.Height;
            previousScale = 1 / Math.Min(xScale, yScale);
            origRules = currentRules = (routeDrawing.Width * previousScale);
            if (routeDrawing.Width > (ViewWindow.Width / previousScale))
            {
                Distant2Origin.Width = ((int)(routeDrawing.Width - (ViewWindow.Width / previousScale)) / 2) * previousScale;
            }
            if (routeDrawing.Height > (ViewWindow.Height / previousScale))
            {
                Distant2Origin.Height = ((int)(routeDrawing.Height - (ViewWindow.Height / previousScale)) / 2) * previousScale;
            }
            Distant2Origin.Width += 1024f;  //  Origin is on center of tile
            Distant2Origin.Height += 1024f;  //  Origin is on center of tile
            ComputeUsedScale();
            ComputeStartDrawing();
            return;
        }

        public void InitImage()
        {
            routeDrawing.Width = IM_Width;
            routeDrawing.Height = IM_Height;
            correctY.Height = IM_Height;

            if (routeDrawing.Image != null)
            {
                routeDrawing.Image.Dispose();
            }
            routeDrawing.Image = new Bitmap(routeDrawing.Width, routeDrawing.Height);
        }

        [CallOnThread("Updater")]
        void ActivityTimer_Tick(object sender, EventArgs e)
        {
            GenerateView();
        }

        public List<TilesInfo> getTileList()
        {
            return tilesList;
        }

        public float getUsedScale()
        {
            return usedScale;
        }
    #endregion

    #region Draw

        public bool firstShow = true;
        public bool followTrain = false;
        float subX, subY;
        float oldWidth = 0;
        float oldHeight = 0;

        public void needRedim()
        {
            if (oldHeight != this.routeDrawing.Height || oldWidth != this.routeDrawing.Width)
            {
                oldWidth = this.routeDrawing.Width;
                oldHeight = this.routeDrawing.Height;
                IM_Width = this.routeDrawing.Width;
                IM_Height = this.routeDrawing.Height;
                correctY.Height = IM_Height;
                if (routeDrawing.Image != null)
                {
                    routeDrawing.Image.Dispose();
                }
                //  TODO: Check this when the forms is minimized
                if (routeDrawing.Width <= 0 || routeDrawing.Height <= 0)
                   routeDrawing.Image = new Bitmap(1, 1);
                else
                   routeDrawing.Image = new Bitmap(routeDrawing.Width, routeDrawing.Height);
                routePicture = routeDrawing.Size;
                firstShow = true;
            }
        }

        private void ComputeUsedScale()
        {
            previousScale = usedScale;  //  Keep the last usedScale
            usedScale = currentRules / routeDrawing.Width;
            usedScale = (float)Math.Round((double)usedScale, 2, MidpointRounding.AwayFromZero);
        }

        public void GenerateView()
        {

            if (routeDrawing.Image == null) InitImage();
            needRedim();
            //this.routeDrawing.Select();
            using (Graphics g = Graphics.FromImage(routeDrawing.Image))
            {
                Graphics gMod = Graphics.FromImage(routeDrawing.Image);
                subX = 0; // areaRoute.getMinX() + ViewWindow.X; 
                subY = 0; // areaRoute.getMinY() + ViewWindow.Y;
                g.Clear(Color.White);
                ComputeStartDrawing();
                
                usedScale = (currentRules / routeDrawing.Width);
                usedScale = (float)Math.Round((double)usedScale, 2, MidpointRounding.AwayFromZero);
                if (usedScale == 0)
                    usedScale = 0.1f;
                if (Program.aePreference.ShowRuler)
                    showRules(g, usedScale);
                snapSize = (double)(Program.aePreference.getSnapCircle() * usedScale);
                
                PointF[] points = new PointF[3];
                Pen p = darkGrayPen;
                Pen t = bluePen;
                Pen r = redPen;
                Pen pen = bluePen;

                p.Width = 1;
                if (p.Width < 1) p.Width = 1;
                else if (p.Width > 3) p.Width = 3;

                PointF scaledA = new PointF(0, 0);
                PointF scaledB = new PointF(0, 0);
                SizeF decal = (correctY - StartDrawing); 
                PointF scaledC = new PointF(0, 0);
                PointF centerCurve = new PointF(0, 0);
                #region showItem

                pen = bluePen;
                if (Program.aePreference.ShowTiles)
                {   //  X,Y tile are on center of tile
                    foreach (var tile in tilesList)
                    {
                        scaledA.X = decal.Width + (((tile.TileX * 2048f) - subX -1024f) / usedScale);
                        scaledA.Y = decal.Height - (((tile.TileZ *  2048f) - subY + 1024f) / usedScale);
                        int width = (int)(2048 / usedScale);
                        int height = (int)(2048 / usedScale);
                        g.DrawRectangle(pen, (int)scaledA.X, (int)scaledA.Y, width, height);
                        g.DrawString(" " + tile.TileX + "," + tile.TileZ, sidingFont, sidingBrush, scaledA.X, scaledA.Y);
                    }
                }
                if (Program.aePreference.ShowTrackInfo && Program.aePreference.PlSiZoom >= usedScale)
                {
                    foreach (var buffer in Simulator.mstsItems.getBuffers())
                    {
                        buffer.SynchroLocation();
                        scaledA.X = decal.Width + (((float)buffer.Location.X - subX) / usedScale);
                        scaledA.Y = decal.Height - (((float)buffer.Location.Y - subY) / usedScale);
                        g.DrawRectangle(pen, (int)scaledA.X, (int)scaledA.Y, 5, 5);
                        g.DrawString((string)buffer.associateNode.Index.ToString(), sidingFont, sidingBrush, scaledA.X - 20, scaledA.Y);
                        if (Program.aePreference.ShowPlSiLabel && buffer.isItSeen())
                        {
                            System.Drawing.Drawing2D.Matrix matrixSA = new System.Drawing.Drawing2D.Matrix();
                            matrixSA.RotateAt((float)0.0f, new System.Drawing.Point((int)scaledA.X, (int)scaledA.Y), MatrixOrder.Append);
                            g.Transform = matrixSA;
                            g.DrawString(buffer.NameBuffer, sidingFont, sidingBrush, scaledA.X + 15, scaledA.Y + 10);
                        }

                    }
                     foreach (var junction in aeItems.getSwitches())
                    {
                        junction.SynchroLocation();
                        scaledA.X = decal.Width + (((float)junction.Location.X - subX) / usedScale);
                        scaledA.Y = decal.Height - (((float)junction.Location.Y - subY) / usedScale);
                        g.DrawRectangle(pen, (int)scaledA.X, (int)scaledA.Y, 5, 5);
                        g.DrawString((string)junction.associateNode.Index.ToString(), sidingFont, sidingBrush, scaledA.X - 20, scaledA.Y);
                    }
                }

                foreach (var line in aeItems.getSegments())
                //foreach (var line in shape.getTrItems())
                {
                    scaledA.X = decal.Width + (((float)line.getStart().X - subX) / usedScale);
                    scaledA.Y = decal.Height - (((float)line.getStart().Y - subY) / usedScale);
                    scaledB.X = decal.Width + ((float)line.getEnd().X - subX) / usedScale;
                    scaledB.Y = decal.Height - (((float)line.getEnd().Y - subY) / usedScale);

                    if ((scaledA.X < 0 && scaledB.X < 0) || (scaledA.X > IM_Width && scaledB.X > IM_Width) || (scaledA.Y > IM_Height && scaledB.Y > IM_Height) || (scaledA.Y < 0 && scaledB.Y < 0))
                        continue;
                    if (line.isCurved == true && line.curve != null)
                    {
                        centerCurve.X = decal.Width + (((float)line.curve.Centre.ConvertVector().X - subX) / usedScale);
                        centerCurve.Y = decal.Height - (((float)line.curve.Centre.ConvertVector().Y - subY) / usedScale);
                        scaledC.X = decal.Width + ((float)line.curve.C.ConvertVector().X - subX) / usedScale;
                        scaledC.Y = decal.Height - ((float)line.curve.C.ConvertVector().Y - subY) / usedScale;
                        points[0] = scaledA;
                        points[1] = scaledC;
                        points[2] = scaledB;
                        if (line.isSnap())
                        {
                            if (Program.aePreference.ShowSnapLine && Program.aePreference.PlSiZoom >= usedScale)
                            {
                                g.DrawRectangle(pen, (int)scaledA.X, (int)scaledA.Y, 5, 5);
                                g.DrawRectangle(pen, (int)scaledB.X, (int)scaledB.Y, 5, 5);
                                g.DrawRectangle(pen, (int)scaledC.X, (int)scaledC.Y, 5, 5);
                                if (Program.aePreference.ShowSnapInfo)
                                {
                                    g.DrawRectangle(pen, (int)centerCurve.X, (int)centerCurve.Y, 5, 5);
                                    g.DrawLine(pen, centerCurve, scaledA);
                                    g.DrawLine(pen, centerCurve, scaledB);
                                    if (line.GetDecal())
                                    {
                                        g.DrawString(line.AsString() + "a", sidingFont, sidingBrush, (int)scaledA.X + 30, (int)scaledA.Y + 10);
                                        g.DrawString(line.AsString() + "b", sidingFont, sidingBrush, (int)scaledB.X + 30, (int)scaledB.Y - 10);
                                    }
                                    else
                                    {
                                        g.DrawString(line.AsString() + "a", sidingFont, sidingBrush, (int)scaledA.X, (int)scaledA.Y + 10);
                                        g.DrawString(line.AsString() + "b", sidingFont, sidingBrush, (int)scaledB.X, (int)scaledB.Y - 10);
                                    }
                                    if (line.curve.checkedPoint != null)
                                    {
                                        foreach (var checkP in line.curve.checkedPoint)
                                        {
                                            scaledC.X = decal.Width + ((float)checkP.ConvertVector().X - subX) / usedScale;
                                            scaledC.Y = decal.Height - ((float)checkP.ConvertVector().Y - subY) / usedScale;
                                            g.DrawRectangle(pen, (int)scaledC.X, (int)scaledC.Y, 5, 5);
                                        }
                                    }
                                }
                            }
                            g.DrawCurve(r, points);
                        }
                        else
                            g.DrawCurve(p, points);
                    }
                    else
                    {
                        if (line.isSnap())
                        {
                            if (Program.aePreference.ShowSnapLine && Program.aePreference.PlSiZoom >= usedScale)
                            {
                                g.DrawRectangle(pen, (int)scaledA.X, (int)scaledA.Y, 5, 5);
                                g.DrawRectangle(pen, (int)scaledB.X, (int)scaledB.Y, 5, 5);
                                if (Program.aePreference.ShowSnapInfo)
                                {
                                    if (line.GetDecal())
                                    {
                                        g.DrawString(line.AsString() + "a", sidingFont, sidingBrush, (int)scaledA.X + 30, (int)scaledA.Y + 20);
                                        g.DrawString(line.AsString() + "b", sidingFont, sidingBrush, (int)scaledB.X + 30, (int)scaledB.Y - 20);
                                    }
                                    else
                                    {
                                        g.DrawString(line.AsString() + "a", sidingFont, sidingBrush, (int)scaledA.X, (int)scaledA.Y + 10);
                                        g.DrawString(line.AsString() + "b", sidingFont, sidingBrush, (int)scaledB.X, (int)scaledB.Y - 10);
                                    }
                                }
                            }
                            g.DrawLine(r, scaledA, scaledB);
                        }
                        else
                            g.DrawLine(p, scaledA, scaledB);
                    }
                }

                foreach (var item in aeConfig.getTagWidgets())
                {
                    var tag = item.tagWidget;
                    scaledA.X = decal.Width + (tag.Location.X - subX) / usedScale;
                    scaledA.Y = (decal.Height - ((tag.Location.Y - subY) / usedScale)) - 16;
                    tag.SynchroLocation();
                    tag.Location2D.X = scaledA.X; tag.Location2D.Y = scaledA.Y;
                    if ((scaledA.X < 0) || (scaledA.X > IM_Width) || (scaledA.Y > IM_Height) || (scaledA.Y < 0))
                        continue;

                    if (item.NameDisplay.Checked)
                    {
                        g.DrawString(item.TagName.Text, sidingFont, sidingBrush, scaledA);
                    }
                    else
                    {
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)scaledA.X, (int)scaledA.Y, 16, 16);
                        g.DrawIcon(TagIcon, rect);
                    }
                    if (item.selectedTag)
                    {
                        item.TagName.SelectAll();
                    }
                }
                foreach (var item in aeConfig.getStationWidgets())
                {
                    System.Drawing.Drawing2D.Matrix myMatrix = new System.Drawing.Drawing2D.Matrix();
                    var station = item.stationWidget;
                    station.SynchroLocation();
                    scaledA.X = (decal.Width + (station.Location.X - subX) / usedScale);
                    scaledA.Y = (decal.Height - ((station.Location.Y - subY) / usedScale));
                    station.Location2D.X = scaledA.X; station.Location2D.Y = scaledA.Y;

                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)scaledA.X,
                                                        (int)scaledA.Y, 
                                                        (int)(10/usedScale), (int)(10/usedScale));
                    if (item.stationWidget.icoAngle != 0f)
                    {
                        myMatrix.RotateAt(item.stationWidget.icoAngle, scaledA, MatrixOrder.Append);
                        gMod.Transform = myMatrix;
                    }
                    gMod.DrawIcon(StationIcon, rect);
                    gMod.ResetTransform();
                    if (station.isItSeen())
                    {
                        g.DrawEllipse(r, scaledA.X - 4, scaledA.Y - 4, 8, 8);
                    }
                    else
                        g.DrawEllipse(t, scaledA.X - 4, scaledA.Y - 4, 8, 8);

                    if (station.stationArea.Count > 1)
                    {
                        bool canDraw = false;
                        foreach (StationAreaItem SAWidget in station.stationArea)
                        {
                            int X = (int)(decal.Width + ((SAWidget.Coord.TileX * 2048f + SAWidget.Coord.X - subX) / usedScale));
                            int Y = (int)(decal.Height - ((SAWidget.Coord.TileY * 2048f + SAWidget.Coord.Y - subY) / usedScale));
                            if (!((X < 0) || (X > IM_Width) || (Y > IM_Height) || (Y < 0)))
                            {
                                canDraw = true;
                                break;
                            }
                        }
                        if (((scaledA.X < 0) || (scaledA.X > IM_Width) || (scaledA.Y > IM_Height) || (scaledA.Y < 0)) && !canDraw)
                            continue;

                        List<System.Drawing.Point> polyPoints = new List<System.Drawing.Point>();
                        foreach (StationAreaItem SAWidget in station.stationArea)
                        {
                            int X = (int)(decal.Width + ((SAWidget.Coord.TileX * 2048f + SAWidget.Coord.X - subX) / usedScale));
                            int Y = (int)(decal.Height - ((SAWidget.Coord.TileY * 2048f + SAWidget.Coord.Y - subY) / usedScale));
                            polyPoints.Add(new System.Drawing.Point(X, Y));
                            if (SAWidget.IsInterface())
                            {
                                System.Drawing.Drawing2D.Matrix matrixSA = new System.Drawing.Drawing2D.Matrix();

                                rect = new System.Drawing.Rectangle((int)X, 
                                    (int)(Y - (2 / usedScale)), 
                                    (int)(2/usedScale), (int)(2/usedScale));
                                if (SAWidget.getStationConnector().getDirConnector() == AllowedDir.IN)
                                {
                                    matrixSA.RotateAt((float)SAWidget.getStationConnector().angle, new System.Drawing.Point(X, Y), MatrixOrder.Append);
                                    gMod.Transform = matrixSA;
                                    gMod.DrawIcon(StationConnector, rect);
                                }
                                else if (SAWidget.getStationConnector().getDirConnector() == AllowedDir.OUT)
                                {
                                    matrixSA.RotateAt((float)SAWidget.getStationConnector().angle + 180f, new System.Drawing.Point(X, Y), MatrixOrder.Append);
                                    gMod.Transform = matrixSA;
                                    gMod.DrawIcon(StationConnector, rect);
                                }
                                else if (SAWidget.getStationConnector().getDirConnector() == AllowedDir.InOut)
                                {
                                    matrixSA.RotateAt((float)SAWidget.getStationConnector().angle, new System.Drawing.Point(X, Y), MatrixOrder.Append);
                                    gMod.Transform = matrixSA;
                                    gMod.DrawIcon(StationConnector, rect);
                                    matrixSA.RotateAt(180f, new System.Drawing.Point(X, Y), MatrixOrder.Append);
                                    gMod.Transform = matrixSA;
                                    gMod.DrawIcon(StationConnector, rect);
                                }
                                gMod.ResetTransform();
                                //else
                                {
                                    if (SAWidget.getStationConnector().isConfigured())
                                    {
                                        System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(Color.FromArgb(100, Color.Red));
                                        g.FillEllipse(myBrush, (float)(X - 4), (float)(Y - 4), (float)8, (float)8);
                                        myBrush.Dispose();
                                    }
                                    else
                                    {
                                        g.DrawEllipse(Pens.Red, X - 4, Y - 4, 8, 8);
                                    }
                                    if (SAWidget.getStationConnector().getLabel().Length > 0 && 
                                        Program.aePreference.ShowPlSiLabel && Program.aePreference.PlSiZoom >= usedScale &&
                                        SAWidget.isItSeen())
                                        g.DrawString(SAWidget.getStationConnector().getLabel(), stationFont, sidingBrush, new System.Drawing.Point(X+15, Y+10));
                                }
                            }
                            else
                                g.DrawEllipse(Pens.DarkBlue, X - 1, Y - 1, 3, 3);
                        }

                        using (SolidBrush br = new SolidBrush(Color.FromArgb(100, Color.Yellow)))
                        {
                            g.FillPolygon(br, polyPoints.ToArray());
                        }
                        g.DrawPolygon(Pens.DarkBlue, polyPoints.ToArray());
                    }
                    if (item.NameDisplay.Checked)
                    {
                        if ((scaledA.X < 0) || (scaledA.X > IM_Width) || (scaledA.Y > IM_Height) || (scaledA.Y < 0))
                            continue;
                        g.DrawString(item.StationName.Text, stationFont, sidingBrush, scaledA);
                    }

                    if (item.selectedStation)
                    {
                        item.StationName.SelectAll();
                    }
                }

                foreach (var item in aeConfig.getActItem())
                {
                    if (item is PathEventItem)
                    {
                        PointF coord = ((PathEventItem)item).Coord.ConvertToPointF();
                        scaledA.X = (decal.Width + (coord.X - subX) / usedScale) -8;
                        scaledA.Y = (decal.Height - ((coord.Y - subY) / usedScale)) -8;
                        if ((scaledA.X < 0) || (scaledA.X > IM_Width) || (scaledA.Y > IM_Height) || (scaledA.Y < 0))
                            continue;

                        if (((PathEventItem)item).nameVisible)
                        {
                            g.DrawString(((PathEventItem)item).nameEvent, sidingFont, sidingBrush, scaledA);
                        }
                        else
                        {
                            System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)scaledA.X, (int)scaledA.Y, 16, 16);
                            g.DrawIcon(((PathEventItem)item).getIcon(), rect);
                        }
                    }
                }
    #endregion

                #region showCursor

                if (findItemFromMouse(CurrentMousePosition) != null)
                {
                    pen = r;
                }
                else
                {
                    pen = t;
                }

                this.Cursor = CursorToUse;
                if (Program.aePreference.ShowSnapCircle && (ToolClicked == ToolClicks.NO_TOOL))
                {
                    PointF point = convertScreen2ViewCoord(CurrentMousePosition.X, CurrentMousePosition.Y);
                    MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
                    int sizeEllipse = (int)(Program.aePreference.getSnapCircle() / usedScale);
                    g.DrawEllipse(pen, (int)(CurrentMousePosition.X - (sizeEllipse / 2)),
                                        (int)(CurrentMousePosition.Y - sizeEllipse / 2),
                                        (int)(sizeEllipse),
                                        (int)(sizeEllipse));
                    g.DrawString(coord.asString(), sidingFont, sidingBrush, CurrentMousePosition.X + 20, CurrentMousePosition.Y - 10);
                }
                else if (ToolClicked == ToolClicks.NO_TOOL)
                {

                }
                else if (ToolClicked != ToolClicks.NO_TOOL)
                {
                    var tool = ToolToUse;
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)(CurrentMousePosition.X),
                                        (int)(CurrentMousePosition.Y - (20)),
                                        (int)20,
                                        (int)20);

                    if (tool != null)
                    {
                        g.DrawImage(tool, rect);
                    }
                }
    #endregion

                float x, y;
                PointF scaledItem = new PointF(0f, 0f);
                var widthDraw = 12f * p.Width;
                if (widthDraw > 20)
                    widthDraw = 20;//not to make it too large
                if (usedScale < Program.aePreference.getPlSiZoom())
                {
#if !DRAW_ALL
#region drawSignal
                    foreach (var s in aeItems.getSignals())
                    {
                        s.SynchroLocation();
                        if (float.IsNaN(s.Location.X) || float.IsNaN(s.Location.Y))
                            continue;
                        x = decal.Width + (s.Location.X - subX) / usedScale;
                        y = decal.Height - (s.Location.Y - subY) / usedScale;

                        if (x < 0 || x > IM_Width || y > IM_Height || y < 0)
                            continue;
                        scaledItem.X = x;
                        scaledItem.Y = y;
                        s.Location2D.X = scaledItem.X;
                        s.Location2D.Y = scaledItem.Y;
                        //if (s.Signal.isSignalNormal())//only show nor
                        {
                            var color = Brushes.Green;
                            pen = greenPen;
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
                            System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)scaledItem.X-12, (int)scaledItem.Y-5, (int)widthDraw, (int)widthDraw);
                            g.DrawEllipse(Pens.DarkBlue, (int)scaledItem.X - 1, (int)scaledItem.Y - 1, 2, 2);
                            if (s.hasDir)
                            {
                                System.Drawing.Point rotationLoc = new System.Drawing.Point((int)scaledItem.X-5, (int)scaledItem.Y+5);
                                System.Drawing.Drawing2D.Matrix myMatrix = new System.Drawing.Drawing2D.Matrix();
                                scaledB.X = decal.Width + (s.Dir.X - subX) / usedScale;
                                scaledB.Y = decal.Height - (s.Dir.Y - subY) / usedScale;
                                float a = ((float)(Math.Atan2(scaledB.Y - scaledItem.Y, scaledB.X - scaledItem.X) * 180.0d / Math.PI)) - 90.0f;
                                //a = ((float)(Math.Atan2(scaledItem.Y - scaledB.Y, scaledItem.X - scaledB.X) * 180.0d / Math.PI)) - 90.0f;
                                //a = ((float)(Math.Atan2(scaledB.X - scaledItem.X, scaledB.Y - scaledItem.Y) * 180.0d / Math.PI)) + 90.0f;
                                //a = -90;
                                myMatrix.RotateAt(a, rotationLoc, MatrixOrder.Append);

                                // Draw the rectangle to the screen again after applying the

                                // transform.
                                g.Transform = myMatrix;
                            }

                            if (Program.aePreference.ShowAllSignal)
                            {
                                switch (s.SigFonction)
                                {
                                    case MstsSignalFunction.NORMAL:
                                    case MstsSignalFunction.DISTANCE:
                                        g.DrawIcon(SignalIco, rect);
                                        break;
                                    case MstsSignalFunction.REPEATER:
                                        g.DrawIcon(RepeatIco, rect);
                                        break;
                                    case MstsSignalFunction.SHUNTING:
                                        g.DrawIcon(ShuntIco, rect);
                                        break;
                                    case MstsSignalFunction.ALERT:
                                        g.DrawIcon(OtherSigIco, rect);
                                        break;
                                    case MstsSignalFunction.SPEED:
                                        g.DrawIcon(SpeedIco, rect);
                                        break;
                                }
                            }
                            else
                            {
                                switch (s.SigFonction)
                                {
                                    case MstsSignalFunction.NORMAL:
                                    case MstsSignalFunction.DISTANCE:
                                        g.DrawIcon(SignalIco, rect);
                                        break;
                                }
                            }
                            g.ResetTransform();
                        }
                    }
#endregion
#region crossOver
                    foreach (AECrossOver crossOver in aeItems.getCrossOver())
                    {
                        System.Drawing.Drawing2D.Matrix matrixSA = new System.Drawing.Drawing2D.Matrix();
                        if (float.IsNaN(crossOver.Location.X) || float.IsNaN(crossOver.Location.Y))
                            continue;
                        x = decal.Width + (crossOver.Location.X - subX) / usedScale;
                        y = decal.Height - (crossOver.Location.Y - subY) / usedScale;

                        if (x < 0 || x > IM_Width || y > IM_Height || y < 0)
                            continue;
                        double tempo = 0;

                        g.DrawRectangle(pen, (int)x, (int)y, 5, 5);
                        if (Program.aePreference.ShowPlSiLabel && crossOver.isItSeen())
                        {
                            matrixSA.RotateAt((float)tempo, new System.Drawing.Point((int)x, (int)y), MatrixOrder.Append);
                            g.Transform = matrixSA;
                        }
                        g.ResetTransform();
                    }
                    #endregion

#region drawSiding
                    foreach (SideItem siding in aeItems.getSidings())
                    {
                        System.Drawing.Drawing2D.Matrix matrixSA = new System.Drawing.Drawing2D.Matrix();
                        if (float.IsNaN(siding.Location.X) || float.IsNaN(siding.Location.Y))
                            continue;
                        x = decal.Width + (siding.Location.X - subX) / usedScale;
                        y = decal.Height - (siding.Location.Y - subY) / usedScale;

                        if (x < 0 || x > IM_Width || y > IM_Height || y < 0)
                            continue;
                        //x2 = decal.Width + (siding.Location2.X - subX) / usedScale;
                        //y2 = decal.Height - (siding.Location2.Y - subY) / usedScale;
                        //siding.Location2D.X = x;
                        //siding.Location2D.Y = y;
                        double tempo = 0;
                        //double tempo = Math.Atan2(y2 - y, x2 - x);
                        //tempo = (tempo * 180.0d) / Math.PI;

                        if (siding.type == TrItem.trItemType.trSIDING)
                        {
                            g.DrawEllipse(Pens.Blue, x - 1, y - 1, 3, 3);
                            //g.DrawEllipse(Pens.Blue, x2 - 1, y2 - 1, 3, 3);
                            if (Program.aePreference.ShowPlSiLabel && siding.isItSeen())
                            {
                                matrixSA.RotateAt((float)tempo, new System.Drawing.Point((int)x, (int)y), MatrixOrder.Append);
                                g.Transform = matrixSA;
                                g.DrawString(siding.Name, sidingFont, sidingBrush, x+15, y+10);
                            }
                        }
                        else
                        {
                            g.DrawEllipse(Pens.Brown, x - 1, y - 1, 3, 3);
                            //g.DrawEllipse(Pens.Brown, x2 - 1, y2 - 1, 3, 3);
                            if (Program.aePreference.ShowPlSiLabel && siding.isItSeen())
                            {
                                matrixSA.RotateAt((float)tempo, new System.Drawing.Point((int)x, (int)y), MatrixOrder.Append);
                                g.Transform = matrixSA;
                                g.DrawString(siding.Name, sidingFont, sidingBrush, x+15, y+10);
                            }
                        }
                        g.ResetTransform();
                    }
                }
#endregion
#endif
            }
            routeDrawing.Invalidate();
        }

        void showRules(Graphics g, float usedScale)
        {
            PointF scaledA = new PointF(0, 0);
            PointF scaledB = new PointF(0, 0);
            SizeF decal = (correctY - StartDrawing);
            System.Drawing.Drawing2D.Matrix myMatrix = new System.Drawing.Drawing2D.Matrix();

            scaledA.X = 10;
            scaledA.Y = 10;
            scaledB.X = scaledA.X + (100 / usedScale);
            scaledB.Y = scaledA.Y;
            g.DrawLine(greenPen, scaledA, scaledB);

            scaledB.X = scaledA.X;
            scaledB.Y = scaledA.Y + (100 / usedScale);
            g.DrawLine(greenPen, scaledA, scaledB);
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)10,
                                    (int)10,
                                    (int)(100 / usedScale), (int)(100 / usedScale));
            //g.DrawIcon(Ruler, rect);
            g.DrawImage(Ruler, rect);
            scaledA.X = 30;
            scaledA.Y = 30;
            g.DrawString("100m", stationFont, sidingBrush, scaledA);
#if false
		            scaledA.X = 0;
            scaledA.Y = 0;
            
            myMatrix.RotateAt(90f, scaledA, MatrixOrder.Append);
            g.Transform = myMatrix;
            g.DrawIcon(Ruler, rect);
            g.ResetTransform();
  
	#endif        
        }
#endregion

        private void ComputeStartDrawing()
        {
            var d2o = Distant2Origin;
            StartDrawing = Size.Round(d2o);
            StartDrawing.Width = -(d2o.Width / usedScale);
            StartDrawing.Width = (float)Math.Round((double)StartDrawing.Width, 2, MidpointRounding.AwayFromZero);
            StartDrawing.Height = (d2o.Height / usedScale);
            StartDrawing.Height = (float)Math.Round((double)StartDrawing.Height, 2, MidpointRounding.AwayFromZero);
        }


        #region MouseEvent

        private bool zoomed  = false;
        private void routeDrawingMouseDown(object sender, MouseEventArgs e)
        {
            bool shiftKey = false;
            if (e.Button == MouseButtons.Left)
            {
                LeftClick = true;
            }
            if (e.Button == MouseButtons.Right)
            {
                RightClick = true;
            }
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                shiftKey = true;
            }

            if (LeftClick == true && RightClick == false)
            {
                PointF point = convertScreen2ViewCoord(e.X, e.Y);
                MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);

                LeftMouseDown(sender, e, shiftKey);
            }
            else if (LeftClick == false && RightClick == true)
            {
                RightMouseDown(sender, e, shiftKey);
            }
            LastCursorPosition.X = e.X;
            LastCursorPosition.Y = e.Y;
        }

        private void LeftMouseDown(object sender, MouseEventArgs e, bool shiftKey)
        {
            //this.routeDrawing.Focus();
            Dragging = false;
            if (ToolClicked == ToolClicks.AREA_ADD &&
                itemToUpdate != null &&
                (itemToUpdate.GetType() == typeof(StationItem)))
            {
                PointF point = convertScreen2ViewCoord(e.X, e.Y);
                MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
                StationAreaItem info = aeConfig.AddPointArea((StationItem)itemToUpdate, coord);
                if (info != null)
                {
                    aeConfig.AddORItem(info);
                    stationItem = (StationItem)itemToUpdate;
                    itemToUpdate = info;
                    
                }
                else
                {
                    SetToolClicked(ToolClicks.NO_TOOL);
                }
            }
            else
            {
                var item = findItemFromEvent(e);
                if (ToolClicked == ToolClicks.NO_TOOL &&
                    item != null && item.IsMovable())
                {
                    SetToolClicked(ToolClicks.MOVE);
                }
                if ((ToolClicked == ToolClicks.MOVE
                    || ToolClicked == ToolClicks.ROTATE) &&
                    item != null)
                {
                    //CancelOperation();
                    if (itemToUpdate != item)
                    {
                        itemToUpdate = null;
                        if (ToolClicked == ToolClicks.MOVE && item.IsMovable())
                        {
                            itemToUpdate = item;
                        }
                        else if (ToolClicked == ToolClicks.ROTATE && item.IsRotable())
                        {
                            itemToUpdate = item;
                        }
                    }
                }
                else if (ToolClicked == ToolClicks.AREA && item != null)
                {
                    //CancelOperation();      // First, cancel all operation
                    itemToUpdate = item;
                }
                else if (Dragging == false && ToolClicked == ToolClicks.NO_TOOL)
                {
                    Dragging = true;
                    SetToolClicked(ToolClicks.DRAG);
                }
            }

        }

        private void RightMouseDown(object sender, MouseEventArgs e, bool shiftKey)
        {
            //this.routeDrawing.Focus();
            var item = findItemFromEvent(e);
            itemToEdit = null;
            if (ToolClicked == ToolClicks.NO_TOOL &&
                item != null && item.IsEditable())
            {
                itemToEdit = item;
            }
            else if (Zooming == false)
            {
                Zooming = true;
                refZoomPoint.X = e.X;
                refZoomPoint.Y = e.Y;
                PointF point = convertScreen2ViewCoord(refZoomPoint.X, refZoomPoint.Y);
                coordZoomPoint = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
                refZoomPoint.Y = routeDrawing.Height - e.Y;
                ToolClicked = ToolClicks.ZOOM;
                System.Drawing.Point refCoord = convertViewCoord2Screen(coordZoomPoint);
            }
        }


        private void routeDrawingMouseMove(object sender, MouseEventArgs e)
        {
            bool controlKey = false;
            Program.actEditor.DisplayStatusMessage((string)("Mouse move: " + e.X + "," + e.Y));
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                controlKey = true;
            }


            if (ToolClicked == ToolClicks.AREA_ADD &&
                itemToUpdate != null &&
                (itemToUpdate.GetType() == typeof(StationAreaItem)))
            {
                PointF point = convertScreen2ViewCoord(e.X, e.Y);
                MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
                aeConfig.UpdateItem (itemToUpdate, coord, controlKey, false);
                //((StationAreaWidget)itemToUpdate).UpdatePointArea(aeConfig.getSegments(), );
            }

            if (!Dragging && !Zooming)
            {
                CurrentMousePosition.X = e.X;
                CurrentMousePosition.Y = e.Y;
                if (ToolClicked == ToolClicks.MOVE 
                        && itemToUpdate != null 
                        && itemToUpdate.IsMovable())
                {
                    if (e.X < 10 || e.Y < 10 || e.X > routePicture.Width - 10 || e.Y > routePicture.Height - 10)
                    {
                        dragWindow(e, -1);
                    }
                    PointF point = convertScreen2ViewCoord (e.X, e.Y);
                    MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
                    aeConfig.UpdateItem(itemToUpdate, coord, controlKey, false);
                    //itemToUpdate.configCoord(coord, aeConfig.getSegments(), controlKey);
                }
                else if (ToolClicked == ToolClicks.ROTATE
                        && itemToUpdate != null 
                        && itemToUpdate.IsRotable())
                {
                    PointF point = e.Location;
                    double tempo = Math.Atan2(itemToUpdate.Location2D.Y - point.Y, itemToUpdate.Location2D.X - point.X);
                    tempo = (tempo * 180.0d) / Math.PI;
                    tempo = tempo - 90d;
                    itemToUpdate.setAngle((float)tempo);
                    //itemToUpdate.setAngle(((float)(Math.Atan2(itemToUpdate.Location2D.Y - point.Y, itemToUpdate.Location2D.X - point.X) * 180.0d / Math.PI)) - 90.0f);
                }
                GenerateView();
                return;
            }
            if (Dragging && !Zooming)
            {
                dragWindow(e, 1);
                /*
                SizeF diff = new SizeF(LastCursorPosition.X - e.X, e.Y - LastCursorPosition.Y);
                diff.Width *= usedScale;
                diff.Height *= usedScale;
                Distant2Origin -= diff;
                GenerateView();
                 * */
            }
#if SPA_ADD
            else if (Zooming || windowSizeUpDown.Value < 80)
#else
            else if (Zooming && LastCursorPosition.Y != e.Y)
#endif
            {
                float facteur = 0.98F;
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    facteur = 0.90F;
                zoomed = true;
                if (LastCursorPosition.Y - e.Y < 0)
                {
                    currentRules /= facteur;
                }
                else if (LastCursorPosition.Y - e.Y > 0)
                {
                    currentRules *= facteur;
                }
                

                ZoomAndAlignWindow(coordZoomPoint, refZoomPoint);
            }
            LastCursorPosition.X = e.X;
            LastCursorPosition.Y = e.Y;
            GenerateView();
        }

        private void dragWindow(MouseEventArgs e, int sens)
        {
            int eX = e.X;
            int eY = e.Y;
            //  Le test est important pour conserver le drag fonctionnel
            if (eX < 10 || eY < 10 || eX > routePicture.Width - 10 || eY > routePicture.Height - 10)
            {
                if (eY < 10)
                    eY = LastCursorPosition.Y - (5 * sens);
                else if (eY > routePicture.Height - 10)
                    eY = LastCursorPosition.Y + (5 * sens);
                else
                    eY = LastCursorPosition.Y;
                if (eX < 10)
                {
                    eX = LastCursorPosition.X - (5 * sens);
                }
                else if (eX > routePicture.Width - 10)
                {
                    eX = LastCursorPosition.X + (5 * sens);
                }
                else
                {
                    eX = LastCursorPosition.X;
                }
            }

            SizeF diff = new SizeF(LastCursorPosition.X - eX, eY - LastCursorPosition.Y);
            diff.Width *= usedScale;
            diff.Height *= usedScale;
            Distant2Origin -= diff;
            GenerateView();
        }

        private void routeDrawingMouseUp(object sender, MouseEventArgs e)
        {
            if ((ToolClicked == ToolClicks.AREA_ADD ||
                ToolClicked == ToolClicks.MOVE) &&
                itemToUpdate != null && itemToUpdate.GetType() == typeof(StationAreaItem))
            {
                PointF point = convertScreen2ViewCoord(e.X, e.Y);
                MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
                stationItem = (StationItem)aeConfig.UpdateItem(itemToUpdate, coord, false, true);
                itemToUpdate = stationItem;
                if (stationItem != null && !(ToolClicked == ToolClicks.AREA_ADD))
                {
                    stationItem.complete(aeConfig.aeRouteConfig.orRouteConfig,
                        aeConfig.aeItems,
                        Simulator.mstsDataConfig.TileBase);
                }
            }
            else if (ToolClicked == ToolClicks.AREA &&
                itemToUpdate != null && 
                (itemToUpdate.GetType() == typeof(StationItem) ||
                itemToUpdate.GetType() == typeof(StationAreaItem)))
            {
                SetToolClicked(ToolClicks.AREA_ADD);
            }

            if (LeftClick && ToolClicked != ToolClicks.NO_TOOL &&
                ToolClicked != ToolClicks.DRAG &&
                ToolClicked != ToolClicks.ZOOM)
            {
                PointF point = e.Location;
                lastContextMenuPos = point;
                PlaceToolItem();
            }
            if (LeftClick && !Dragging)
            {
                if (LastCursorPosition.X == e.X && LastCursorPosition.Y == e.Y)
                {
#if To_REMOVE
                    var range = 5 * xScale; if (range > 10) range = 10;
                    var temp = findItemFromMouse(e.X, e.Y, (int)range);
                    if (temp != null)
                    {
                        //
                    }
                    else
                    {
                        //switchPickedItem = null; signalPickedItem = null; UnHandleItemPick(); PickedTrain = null; 
                    }
#endif
                }
            }
            if (RightClick && !zoomed && itemToEdit == null)
            {
                PointF point = e.Location;
                lastContextMenuPos = point;
                PathContextMenu.Show(Cursor.Position);
            }
            else if (RightClick && !zoomed && itemToEdit != null && itemToEdit.IsEditable())
            {
                GlobalItem edited = aeConfig.EditItem(itemToEdit);
                if (edited != null && edited.GetType() == typeof(StationItem))
                {
                    ((StationItem)edited).complete(aeConfig.aeRouteConfig.orRouteConfig,
                        aeConfig.aeItems,
                        Simulator.mstsDataConfig.TileBase);
                }
            }
            zoomed = false;
            Dragging = Zooming = false;

            if (e.Button == MouseButtons.Left) LeftClick = false;
            if (e.Button == MouseButtons.Right) RightClick = false;

            if (ToolClicked != ToolClicks.AREA_ADD)
            {
                CancelOperation();
                ToolClicked = ToolClicks.NO_TOOL;
            }
        }

        void routeDrawing_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
            }
        }

        private void routeDrawingMouseWheel(object sender, MouseEventArgs e)
        {
        }

        public void Viewer2D_KeyDown(object sender, KeyEventArgs e)
        {
            SizeF diff;
            //this.routeDrawing.Focus();
            if (e.KeyCode == Keys.Escape)
            {
                CancelAllOperation();
                return;
            }
            else if (e.KeyCode == Keys.Up)
            {
                diff = new SizeF(0, -1);
            }
            else if (e.KeyCode == Keys.Down)
            {
                diff = new SizeF(0, 1);
            }
            else if (e.KeyCode == Keys.Left)
            {
                diff = new SizeF(1, 0);
            }
            else if (e.KeyCode == Keys.Right)
            {
                diff = new SizeF(-1, 0);
            }
            else
                return;
            diff.Width *= usedScale;
            diff.Height *= usedScale;
            Distant2Origin -= diff;
            GenerateView();

        }

        public void routeDrawing_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Right:
                    //action
                    break;
                case Keys.Up:
                case Keys.Left:
                    //action
                    break;
            }
        }
    #endregion

#region AlignView
        private void ZoomViewWindow()
        {
            float width = ViewWindow.Width * (currentRules / origRules);
            float height = ViewWindow.Height * (currentRules / origRules);
            width = (float)Math.Round((double)width, 2, MidpointRounding.AwayFromZero);
            height = (float)Math.Round((double)height, 2, MidpointRounding.AwayFromZero);
            ViewWindow = new RectangleF(0, 0, width, height);
            ComputeUsedScale();
        }

        private void CenterAndZoomWindow ()
        {
            PointF point = convertScreen2ViewCoord(ViewWindow.Width / 2f, ViewWindow.Height / 2f);
            MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);

            ZoomViewWindow();
            GenerateView();
        }

        private void ZoomAndAlignWindow(MSTSCoord coord, PointF point)
        {
            ZoomViewWindow();

            RealignViewWindow(coord, System.Drawing.Point.Truncate (point));
            GenerateView();
        }

        public void CenterViewWindow(MSTSCoord coord)
        {

            PointF point = new PointF(routeDrawing.Width / 2f, routeDrawing.Height / 2f);

            RealignViewWindow(coord, System.Drawing.Point.Truncate(point));
            GenerateView();
        }

        //  RealignViewWindow:
        //  This function try to align the current ViewWindow in order to have the coord under the refPoint
        //  whatever is the zoomFactor
        private void RealignViewWindow(MSTSCoord coord, System.Drawing.Point refPoint)
        {
            System.Drawing.Point refCoord = convertViewCoord2Screen(coord);
            System.Drawing.Point current = refPoint;
            if (current == refCoord)
            {
                //File.AppendAllText(@"C:\temp\AE.txt", "No move needed");
                return;
            }
            var localScale = currentRules / routeDrawing.Width;
            Distant2Origin.Width = (Distant2Origin.Width + ((current.X - refCoord.X) * localScale));
            Distant2Origin.Height = (Distant2Origin.Height) + ((current.Y - refCoord.Y) * localScale);
            return;
        }
        

#endregion

        #region SaveLoad
        public bool Save()
        {
            //aeWidget.saveConfig(wr);
            return false;
        }


        private void CenterView(object sender, EventArgs e)
        {
            Distant2Origin.Width = 0;
            Distant2Origin.Height = 0;
            GenerateView();
        }

        #endregion

        #region Item management
        void AddTag()
        {
            int cnt = rnd.Next(999999);
            ActEditor tmp = Program.actEditor;
            tmp.SuspendLayout();
            PointF point = convertScreen2ViewCoord(lastContextMenuPos.X, lastContextMenuPos.Y);
            MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
            TagItem tag = new TagItem(ViewerMode);
            tag.configCoord(coord);
            tag.setNameTag(cnt);
            aeConfig.AddORItem(tag);
            System.Drawing.Point current = aeConfig.aeRouteConfig.GetTagPanelPosition();

            if (typeof(ActEditor) == ParentForm.GetType())
            {
                TagWidgetInfo newTagWidget = new TagWidgetInfo(this, tag, cnt);
                aeConfig.AddTagWidgetInfo(newTagWidget);
            }
            tmp.ResumeLayout(false);
            tmp.Refresh();
        }

        private void AddStation()
        {
            int cnt = rnd.Next(999999);
            ActEditor tmp = Program.actEditor;
            tmp.SuspendLayout();
            PointF point = convertScreen2ViewCoord(lastContextMenuPos.X, lastContextMenuPos.Y);
            MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(point);
            StationItem station = new StationItem(ViewerMode, Simulator.traveller);
            station.configCoord(coord);
            station.setNameStation(cnt);
            aeConfig.AddORItem(station);
            System.Drawing.Point current = aeConfig.aeRouteConfig.GetStationPanelPosition();

            if (typeof(ActEditor) == ParentForm.GetType())
            {
                StationWidgetInfo newStationWidget = new StationWidgetInfo(this, station, cnt);
                aeConfig.AddStationInfo(newStationWidget);
            }
            tmp.ResumeLayout(false);
            tmp.PerformLayout();
        }

        void AddStationArea()
        {
            int cnt = rnd.Next(999999);
            /*
            ActEditor tmp = Program.actEditor;
            tmp.SuspendLayout();
            PointF point = convertScreen2ViewCoord(lastContextMenuPos.X, lastContextMenuPos.Y);
            MSTSCoord coord = new MSTSCoord();
            coord.Convert(point);
            StationAreaWidget stationArea = new StationAreaWidget();
            stationArea.configCoord(coord);
            System.Drawing.Point current = aeRouteConfig.GetStationPanelPosition();

            if (typeof(ActEditor) == ParentForm.GetType())
            {
                StationWidgetInfo newStationWidget = new StationWidgetInfo(station, cnt);
                stationWidgetInfo.Add(newStationWidget);
                stationGBList.Add(newStationWidget.getStation());
                aeRouteConfig.AddStationPanel(stationGBList.ToArray());
            }
            tmp.ResumeLayout(false);
            tmp.PerformLayout();
             * */

        }

        private void AddActStart (TrackSegment segment, System.Drawing.Point CurrentMousePosition)
        {
            int cnt = rnd.Next(999999);
            double dist;
            PointF closest = new PointF(0, 0);

            ActEditor tmp = Program.actEditor;
            tmp.SuspendLayout();
            PointF point = convertScreen2ViewCoord(CurrentMousePosition.X, CurrentMousePosition.Y);
            dist = DrawUtility.FindDistanceToSegment(point, segment, out closest);
            MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(closest);
            StartActivity newStart = new StartActivity(aeConfig);
            newStart.ShowDialog();
            ActStartItem actStart = new ActStartItem(ViewerMode);
            actStart.configCoord(coord);
            actStart.setNameStart(cnt);
            aeConfig.AddORItem(actStart);
            aeConfig.AddActItem(actStart);
            tmp.ResumeLayout(false);
            tmp.PerformLayout();
        }

        private void AddActStop (TrackSegment segment, System.Drawing.Point CurrentMousePosition)
        {
            int cnt = rnd.Next(999999);
            double dist;
            PointF closest = new PointF(0, 0);
            ActEditor tmp = Program.actEditor;
            tmp.SuspendLayout();
            PointF point = convertScreen2ViewCoord(CurrentMousePosition.X, CurrentMousePosition.Y);
            dist = DrawUtility.FindDistanceToSegment(point, segment, out closest);
            MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(closest);
            tmp.ResumeLayout(false);
            tmp.PerformLayout();
        }

        private void AddActWait (TrackSegment segment, System.Drawing.Point CurrentMousePosition)
        {
            int cnt = rnd.Next(999999);
            double dist;
            PointF closest = new PointF(0, 0);

            ActEditor tmp = Program.actEditor;
            tmp.SuspendLayout();
            PointF point = convertScreen2ViewCoord(CurrentMousePosition.X, CurrentMousePosition.Y);
            dist = DrawUtility.FindDistanceToSegment(point, segment, out closest);
            MSTSCoord coord = Simulator.mstsDataConfig.TileBase.getMstsCoord(closest);
            WaitActivity newWait = new WaitActivity();
            newWait.SW_PlaceValue.Text = (string)"wait" + cnt;
            newWait.ShowDialog();
            ActWaitItem actWait = new ActWaitItem(ViewerMode);
            actWait.configCoord(coord);
            actWait.setNameWait(cnt);
            aeConfig.AddORItem(actWait);
            aeConfig.AddActItem(actWait);
            tmp.ResumeLayout(false);
            tmp.PerformLayout();
        }


        void PlaceToolItem()
        {
            GlobalItem item;

            switch ((int)ToolClicked)
            {
                case (int)ToolClicks.MOVE:
                    break;
                case (int)ToolClicks.TAG:
                    AddTag();
                    break;
                case (int)ToolClicks.STATION:
                    AddStation();
                    break;
                case (int)ToolClicks.AREA_ADD:
                    AddStationArea();
                    break;
                case (int)ToolClicks.START:
                    item = findSegmentFromMouse(CurrentMousePosition);
                    if (item != null &&
                        item.GetType () == typeof (TrackSegment) &&
                        ((TrackSegment)item).isSnap ())
                    {
                        AddActStart ((TrackSegment)item, CurrentMousePosition);
                    }
                    break;
                case (int)ToolClicks.STOP:
                    item = findSegmentFromMouse(CurrentMousePosition);
                    if (item != null &&
                        item.GetType () == typeof (TrackSegment) &&
                        ((TrackSegment)item).isSnap ())
                    {
                        AddActStop ((TrackSegment)item, CurrentMousePosition);
                    }

                    break;
                case (int)ToolClicks.WAIT:
                    item = findSegmentFromMouse(CurrentMousePosition);
                    if (item != null &&
                        item.GetType () == typeof (TrackSegment) &&
                        ((TrackSegment)item).isSnap())
                    {
                        AddActWait((TrackSegment)item, CurrentMousePosition);
                    }

                    break;
                case (int)ToolClicks.ACTION:
                    break;
                case (int)ToolClicks.CHECK:
                    break;
                case (int)ToolClicks.ROTATE:
                    break;
                default:
                    break;
            }
            SetFocus();
            //this.routeDrawing.Focus();
        }

        public void RemoveTag(int cntStr)
        {
            aeConfig.RemoveTag(cntStr);
            //this.routeDrawing.Focus();

        }

        public void RemoveStation(int cntStr)
        {
            //this.routeDrawing.Focus();
            aeConfig.RemoveStation(cntStr);
        }

        public void TagName_TextChanged(object sender, EventArgs e)
        {
            //this.routeDrawing.Focus();
            var trouve = aeConfig.tagWidgetInfo.Find(place => (((TagWidgetInfo)place).TagName.Name == ((TextBox)sender).Name));
            trouve.tagWidget.nameTag = ((TextBox)sender).Text;

        }

        public void CenterTag_Click(object sender, EventArgs e)
        {
            //this.routeDrawing.Focus();
            TagWidgetInfo trouve = aeConfig.tagWidgetInfo.Find(place => (((TagWidgetInfo)place).TagName.Name == ((Button)sender).Name));
            CenterViewWindow(trouve.tagWidget.Coord);

        }

        public void RemoveTag_Click(object sender, EventArgs e)
        {
            //this.routeDrawing.Focus();
            var trouve = aeConfig.tagWidgetInfo.FindIndex(place => (((TagWidgetInfo)place).TagName.Name == ((Button)sender).Name));
            RemoveTag(trouve);

        }
        public void StationName_TextChanged(object sender, EventArgs e)
        {
            //this.routeDrawing.Focus();
            var trouve = aeConfig.stationWidgetInfo.Find(place => (((StationWidgetInfo)place).StationName.Name == ((TextBox)sender).Name));
            trouve.stationWidget.nameStation = ((TextBox)sender).Text;

        }
        public void CenterStation_Click(object sender, EventArgs e)
        {
            //this.routeDrawing.Focus();
            StationWidgetInfo trouve = aeConfig.stationWidgetInfo.Find(place => (((StationWidgetInfo)place).StationName.Name == ((Button)sender).Name));
            CenterViewWindow(trouve.stationWidget.Coord);

        }

        public void RemoveStation_Click(object sender, EventArgs e)
        {
            //this.routeDrawing.Focus();
            var trouve = aeConfig.stationWidgetInfo.FindIndex(place => (((StationWidgetInfo)place).StationName.Name == ((Button)sender).Name));
            RemoveStation(trouve);

        }

        #endregion

        //  ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region utility
        public PointF convertScreen2ViewCoord(float x, float y)
        {
            PointF val = new PointF(0, 0);
            float scale = (float)getUsedScale();   //   
            x = x * scale;
            y = (routeDrawing.Height - y) * scale;
            float tempX = x - Distant2Origin.Width;
            val.X = tempX;
            val.Y = y - Distant2Origin.Height;
            //File.AppendAllText(@"F:\temp\AE.txt", "convertScreen2ViewCoord: x:" + x + " y:" + y);
            //File.AppendAllText(@"F:\temp\AE.txt", " donnent valX: " + val.X + " valY:" + val.Y + "\n");
            return val;
        }

        public System.Drawing.Point convertViewCoord2Screen(MSTSCoord coord)
        {
            return System.Drawing.Point.Truncate((PointF)convertViewCoord2ScreenF(coord));
        }

        public PointF convertViewCoord2ScreenF(MSTSCoord coord)
        {
            PointF val = new PointF(0, 0);
            val.X = (((coord.TileX * 2048f) + coord.X));    // - areaRoute.getMinX());
            val.Y = (((coord.TileY * 2048f) + coord.Y));    // - areaRoute.getMinY());
            val.X += Distant2Origin.Width;
            val.Y += Distant2Origin.Height;
            val.X /= usedScale;
            val.Y /= usedScale;
            //val.Y = routeDrawing.Height - val.Y;
            return val;
        }

        #endregion

        public void UnsetFocus()
        {
            aeConfig.UnsetFocus(ViewerMode);
        }

        public void SetFocus()
        {
            aeConfig.SetFocus(ViewerMode);
        }

        private void CloseViewer(object sender, FormClosingEventArgs e)
        {
            Simulator.StopUpdaterThread();
            aeConfig.Close(ViewerMode);
            actParent.UnselectTools(ViewerMode);
        }

        private void Viewer2D_Resize(Object sender, System.EventArgs e)
        {
        }

        private void routeDrawing_Click(object sender, EventArgs e)
        {
            /*
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.routeDrawingMouseWheel);
            if (LastCursorPosition.X == e.X && LastCursorPosition.Y == e.Y)
            {
                var range = 5 * (int)xScale; if (range > 10) range = 10;
                var temp = findItemFromMouse(e.X, e.Y, range);
            }
            */
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
            GenerateView();
        }

        private RectangleF GetRect(PointF p, float size)
        {
            return new RectangleF(p.X - size / 2f, p.Y - size / 2f, size, size);
        }

        private RectangleF GetRect(PointF p, float sizeX, float sizeY)
        {
            return new RectangleF(p.X - sizeX / 2f, p.Y - sizeY / 2f, sizeX, sizeY);
        }

        void CancelAllOperation()
        {
            CancelOperation();
            Program.actEditor.UnsetToolClick();
        }

        void CancelOperation()
        {
            if (itemToUpdate != null)
                itemToUpdate.complete(aeConfig.aeRouteConfig.orRouteConfig, aeConfig.aeItems, Simulator.mstsDataConfig.TileBase);
            itemToUpdate = null;
            Dragging = false;
            Zooming = false;
            LeftClick = false;
            RightClick = false;
            zoomed = false;
        }

        public string getDescr()
        {
            return aeConfig.getActivityDescr();
        }

        public void setDescr(string info)
        {
            aeConfig.setActivityDescr(info);
        }

        protected void loadIcon()
        {
            Stream st;
            Assembly a = Assembly.GetExecutingAssembly();
            string[] resName = a.GetManifestResourceNames();
            st = a.GetManifestResourceStream("ActivityEditor.icon.tag.ico");
            TagIcon = new System.Drawing.Icon(st);
            st = a.GetManifestResourceStream("ActivityEditor.icon.SignalBox.ico");
            StationIcon = new System.Drawing.Icon(st);
            st = a.GetManifestResourceStream("ActivityEditor.icon.signal.ico");
            SignalIco = new System.Drawing.Icon(st);
            st = a.GetManifestResourceStream("ActivityEditor.icon.signalShunt.ico");
            ShuntIco = new System.Drawing.Icon(st);
            st = a.GetManifestResourceStream("ActivityEditor.icon.signalOther.ico");
            OtherSigIco = new System.Drawing.Icon(st);
            st = a.GetManifestResourceStream("ActivityEditor.icon.speedLimit.ico");
            SpeedIco = new System.Drawing.Icon(st);
            st = a.GetManifestResourceStream("ActivityEditor.icon.signalRepeat.ico");
            RepeatIco = new System.Drawing.Icon(st);
            st = a.GetManifestResourceStream("ActivityEditor.icon.StationConnector.ico");
            StationConnector = new System.Drawing.Icon(st);

            st = a.GetManifestResourceStream("ActivityEditor.icon.Ruler.png");
            Ruler = Image.FromStream(st);

        }

        private GlobalItem findItemFromEvent(MouseEventArgs e)
        {
            PointF pt = convertScreen2ViewCoord(e.X, e.Y);
            GlobalItem item = findItemFromCoord(pt);
            return item;
        }

        private GlobalItem findSegmentFromMouse(System.Drawing.Point CurrentMousePosition)
        {
            GlobalItem item = null;
            if (aeItems != null)
            {
                PointF pt = convertScreen2ViewCoord((float)CurrentMousePosition.X, (float)CurrentMousePosition.Y);
                item = aeItems.findSegmentFromMouse(pt, (double)(Program.aePreference.getSnapCircle()/2));
                // sideItem = aeItems.findSegmentFromMouse(pt, snapSize);
            }
            return item;
        }

        private GlobalItem findSegmentFromCoord(PointF pt)
        {
            GlobalItem item = null;
            if (aeItems != null)
            {
                item = aeItems.findSegmentFromMouse(pt, snapSize);
            }
            return item;
        }

        public GlobalItem findItemFromMouse(PointF point)
        {
            if (Simulator.orRouteConfig == null || aeItems == null)
            {
                return null;
            }
            findSegmentFromMouse(CurrentMousePosition);
            PointF pt = convertScreen2ViewCoord((float)point.X, (float)point.Y);
            //PointF tf2 = new PointF(0f, 0f);
            //GlobalItem componentItem = Simulator.orRouteConfig.findMetadataItem(pt, snapSize, aeItems);
            GlobalItem item = Simulator.orRouteConfig.FindMetadataItem(pt, (double)(Program.aePreference.getSnapCircle()/2), aeItems);
            return item;
        }

        public GlobalItem findItemFromCoord(PointF pt)
        {
            GlobalItem item;
            GlobalItem segmentItem;
            if (Simulator.orRouteConfig == null || aeItems == null)
            {
                return null;
            }
            segmentItem = findSegmentFromMouse(CurrentMousePosition);
            item = Simulator.orRouteConfig.FindMetadataItem(pt, (double)(Program.aePreference.getSnapCircle() / 2), aeItems);
            if (item == null && segmentItem != null && segmentItem.IsEditable())
            {
                return segmentItem;
            }
            //  GlobalItem componentItem = Simulator.orRouteConfig.findMetadataItem(pt, snapSize, aeItems);
            return item;
        }

        private void FindItem(System.Drawing.Point Last, MouseEventArgs e)
        {
            if ((Last.X == e.X) && (Last.Y == e.Y))
            {
                Predicate<GlobalItem> match = null;
                PointF pt = convertScreen2ViewCoord(e.X, e.Y);
                GlobalItem item = findItemFromCoord(pt);
                if ((item != null) && (item.GetType() == typeof(TagItem)))
                {
                    Predicate<TagWidgetInfo> predicate = null;
                    if (match == null)
                    {
                        match = place => ((place.GetType() == typeof(TagItem)) && (((TagItem)place).Location2D.X == ((TagItem)item).Location2D.X)) && (((TagItem)place).Location2D.Y == ((TagItem)item).Location2D.Y);
                    }
                    TagItem info = (TagItem)Simulator.orRouteConfig.AllItems.Find(match);
                    if (info != null)
                    {
                        if (predicate == null)
                        {
                            predicate = place => place.TagName.Name == info.nameTag;
                        }
                        TagWidgetInfo infoTag = aeConfig.findTagWidget(predicate);
                    }
                }
            }
        }

        public void SetToolClicked(ToolClicks info)
        {
            ToolClicked = info;

            this.Cursor = Cursors.Default;
            if (info == ToolClicks.AREA_ADD)
            {   //  To prevent bad setting
                ToolToUse = global::ActivityEditor.Properties.Resources._64;
                CursorToUse = reduit;
            }
            else if (info == ToolClicks.AREA)
            {
                ToolToUse = global::ActivityEditor.Properties.Resources._32;
                CursorToUse = reduit;
            }
            else if (info == ToolClicks.DRAG)
            {
                ToolToUse = null;
                CursorToUse = Cursors.Hand;
            }
            else if (info == ToolClicks.ZOOM)
            {
                ToolToUse = null;
                CursorToUse = Cursors.SizeAll;
            }
            else if (info == ToolClicks.MOVE)
            {
                ToolToUse = global::ActivityEditor.Properties.Resources.object_move;
                CursorToUse = Cursors.Default;
            }
            else if (info == ToolClicks.ROTATE)
            {
                ToolToUse = global::ActivityEditor.Properties.Resources.object_rotate;
                CursorToUse = reduit;
            }
            else if (info == ToolClicks.TAG)
            {
                ToolToUse = global::ActivityEditor.Properties.Resources.tag;
                CursorToUse = reduit;
            }
            else if (info == ToolClicks.METASEGMENT)
            {
                ToolToUse = global::ActivityEditor.Properties.Resources.metasegment;
                CursorToUse = reduit;
            }
            else if (info == ToolClicks.STATION)
            {
                ToolToUse = global::ActivityEditor.Properties.Resources.SignalBox;
                CursorToUse = reduit;
            }
            else
            {
                ToolClicked = ToolClicks.NO_TOOL;
                CursorToUse = Cursors.Default;
                ToolToUse = null;
            }
        }


#if TOREMOVE
        private void rmvButton_Click(object sender, EventArgs e)
        {
        }

        private void leftButton_MouseDown(object sender, MouseEventArgs e)
        {
            LeftButtonDown = true;
        }

        private void leftButton_MouseLeave(object sender, EventArgs e)
        {
            LeftButtonDown = false;
        }

        private void leftButton_MouseUp(object sender, MouseEventArgs e)
        {
            LeftButtonDown = false;
        }

        private void rightButton_MouseUp(object sender, MouseEventArgs e)
        {
            RightButtonDown = false;
        }

        private void rightButton_MouseDown(object sender, MouseEventArgs e)
        {
            RightButtonDown = true;
        }

        private void rightButton_MouseLeave(object sender, EventArgs e)
        {
            RightButtonDown = false;
        }

        private void upButton_MouseUp(object sender, MouseEventArgs e)
        {
            UpButtonDown = false;
        }

        private void upButton_MouseDown(object sender, MouseEventArgs e)
        {
            UpButtonDown = true;
        }

        private void upButton_MouseLeave(object sender, EventArgs e)
        {
            UpButtonDown = false;
        }

        private void downButton_MouseUp(object sender, MouseEventArgs e)
        {
            DownButtonDown = false;
        }

        private void downButton_MouseDown(object sender, MouseEventArgs e)
        {
            DownButtonDown = true;
        }

        private void downButton_MouseLeave(object sender, EventArgs e)
        {
            DownButtonDown = false;
        }
#endif

    }

    public class TagWidgetInfo
    {
        public System.Windows.Forms.GroupBox OneTagGB;
        public System.Windows.Forms.TextBox TagName;
        public System.Windows.Forms.CheckBox NameDisplay;
        public System.Windows.Forms.Button CenterTag;
        public System.Windows.Forms.Button RemoveTag;
        public TagItem tagWidget;
        public bool selectedTag = false;
        private Viewer2D Parent;

        public TagWidgetInfo(Viewer2D parent, TagItem tag, int cnt)
        {
            Parent = parent;
            OneTagGB = new System.Windows.Forms.GroupBox();
            TagName = new System.Windows.Forms.TextBox();
            NameDisplay = new System.Windows.Forms.CheckBox();
            CenterTag = new System.Windows.Forms.Button();
            RemoveTag = new System.Windows.Forms.Button();
            this.OneTagGB.SuspendLayout();

            // 
            // tagName
            // 
            string tagName = "tag" + cnt;
            this.TagName.Location = new System.Drawing.Point(7, 22);
            this.TagName.Name = tagName;
            this.TagName.Size = new System.Drawing.Size(130, 20);
            this.TagName.TabIndex = 0;
            this.TagName.Text = tag.nameTag;
            this.TagName.TextChanged += new System.EventHandler(Parent.TagName_TextChanged);
            this.TagName.KeyDown += new System.Windows.Forms.KeyEventHandler(Parent.Viewer2D_KeyDown);
            //
            //  nameDisplay
            //
            this.NameDisplay.AutoSize = true;
            this.NameDisplay.Location = new System.Drawing.Point(7, 50);
            this.NameDisplay.Name = "checkBox1";
            this.NameDisplay.Size = new System.Drawing.Size(80, 17);
            this.NameDisplay.TabIndex = 9;
            this.NameDisplay.Text = "Display";
            this.NameDisplay.UseVisualStyleBackColor = true;
            this.NameDisplay.Checked = false;
            //
            // CenterTag Button
            //
            this.RemoveTag.Name = tagName;
            this.RemoveTag.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveTag.Location = new System.Drawing.Point(105, 50);
            this.RemoveTag.Size = new System.Drawing.Size(15, 15);
            this.RemoveTag.TabIndex = 9;
            this.RemoveTag.Text = "x";
            this.RemoveTag.UseVisualStyleBackColor = true;
            this.RemoveTag.Click += new System.EventHandler(Parent.RemoveTag_Click);
            //
            // CenterTag Button
            //
            this.CenterTag.Name = tagName;
            this.CenterTag.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CenterTag.Location = new System.Drawing.Point(120, 50);
            this.CenterTag.Size = new System.Drawing.Size(15, 15);
            this.CenterTag.TabIndex = 9;
            this.CenterTag.Text = "v";
            this.CenterTag.UseVisualStyleBackColor = true;
            this.CenterTag.Click += new System.EventHandler(Parent.CenterTag_Click);
            // 
            // OneTagGB
            // 
            this.OneTagGB.Controls.Add(this.TagName);
            this.OneTagGB.Controls.Add(this.NameDisplay);
            this.OneTagGB.Controls.Add(this.CenterTag);
            this.OneTagGB.Controls.Add(this.RemoveTag);
            //this.OneTagGB.Location = new System.Drawing.Point(6, 3);
            this.OneTagGB.Name = "OneTagGB";
            this.OneTagGB.Size = new System.Drawing.Size(145, 68);
            this.OneTagGB.TabIndex = 1;
            this.OneTagGB.TabStop = false;
            this.OneTagGB.Text = "Tag";
            this.OneTagGB.ResumeLayout(false);

            tagWidget = tag;
        }

        public System.Windows.Forms.GroupBox getTag()
        {
            return OneTagGB;
        }

    }

    public class StationWidgetInfo
    {
        public System.Windows.Forms.GroupBox OneStationGB;
        public System.Windows.Forms.TextBox StationName;
        public System.Windows.Forms.CheckBox NameDisplay;
        public System.Windows.Forms.Button CenterStation;
        public System.Windows.Forms.Button RemoveStation;
        public StationItem stationWidget;
        public bool selectedStation = false;
        Viewer2D Parent;

        public StationWidgetInfo(Viewer2D parent, StationItem station, int cnt)
        {
            Parent = parent;
            OneStationGB = new System.Windows.Forms.GroupBox();
            StationName = new System.Windows.Forms.TextBox();
            NameDisplay = new System.Windows.Forms.CheckBox();
            CenterStation = new System.Windows.Forms.Button();
            RemoveStation = new System.Windows.Forms.Button();

            // 
            // tagName
            // 
            string stationName = "st" + cnt;
            this.StationName.Location = new System.Drawing.Point(7, 22);
            this.StationName.Name = stationName;
            this.StationName.Size = new System.Drawing.Size(130, 20);
            this.StationName.TabIndex = 0;
            this.StationName.Text = station.nameStation;
            this.StationName.TextChanged += new System.EventHandler(Parent.StationName_TextChanged);
            this.StationName.KeyDown += new System.Windows.Forms.KeyEventHandler(Parent.Viewer2D_KeyDown);
            //
            //  nameDisplay
            //
            this.NameDisplay.AutoSize = true;
            this.NameDisplay.Location = new System.Drawing.Point(7, 50);
            this.NameDisplay.Name = "checkBox1";
            this.NameDisplay.Size = new System.Drawing.Size(80, 17);
            this.NameDisplay.TabIndex = 9;
            this.NameDisplay.Text = "Display";
            this.NameDisplay.UseVisualStyleBackColor = true;
            this.NameDisplay.Checked = false;
            // 
            // OneTagGB
            // 
            this.OneStationGB.Controls.Add(this.StationName);
            this.OneStationGB.Controls.Add(this.NameDisplay);
            this.OneStationGB.Controls.Add(this.CenterStation);
            this.OneStationGB.Controls.Add(this.RemoveStation);
            this.OneStationGB.Name = "OneStationGB";
            this.OneStationGB.Size = new System.Drawing.Size(145, 68);
            this.OneStationGB.TabIndex = 1;
            this.OneStationGB.TabStop = false;
            this.OneStationGB.Text = "Station";
            this.OneStationGB.ResumeLayout(false);
            //
            // CenterTag Button
            //
            this.RemoveStation.Name = stationName;
            this.RemoveStation.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveStation.Location = new System.Drawing.Point(105, 50);
            this.RemoveStation.Size = new System.Drawing.Size(15, 15);
            this.RemoveStation.TabIndex = 9;
            this.RemoveStation.Text = "x";
            this.RemoveStation.UseVisualStyleBackColor = true;
            this.RemoveStation.Click += new System.EventHandler(Parent.RemoveStation_Click);
            //
            // CenterTag Button
            //
            this.CenterStation.Name = stationName;
            this.CenterStation.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CenterStation.Location = new System.Drawing.Point(120, 50);
            this.CenterStation.Size = new System.Drawing.Size(15, 15);
            this.CenterStation.TabIndex = 9;
            this.CenterStation.Text = "v";
            this.CenterStation.UseVisualStyleBackColor = true;
            this.CenterStation.Click += new System.EventHandler(Parent.CenterStation_Click);

            stationWidget = station;
        }
        public System.Windows.Forms.GroupBox getStation()
        {
            return OneStationGB;
        }

    }
}
