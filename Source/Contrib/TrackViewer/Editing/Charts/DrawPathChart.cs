// COPYRIGHT 2015 by the Open Rails project.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using Orts.Formats.Msts;
using ORTS.Common;

using ORTS.TrackViewer.Drawing;

namespace ORTS.TrackViewer.Editing.Charts
{
    #region DrawPathChart
    /// <summary>
    /// Wrapper class for drawing charts of paths. This class make sure the data is updated, and also the actual window is shown or closed as needed
    /// </summary>
    public class DrawPathChart
    {
        // Injection dependencies
        private PathEditor pathEditor;
        private ORTS.TrackViewer.Drawing.RouteData routeData;
        
        //
        private PathChartData pathData;
        private PathChartWindow chartWindow;
        private ISubChart heightChart;
        private ISubChart gradeChart;
        private ISubChart curvatureChart;

        private bool ChartWindowIsOpen { get { return chartWindow.Visibility == Visibility.Visible; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public DrawPathChart()
        {
            chartWindow = new PathChartWindow();
            TrackViewer.Localize(chartWindow);
        }

        /// <summary>
        /// Set the route and path-specifics.
        /// </summary>
        /// <param name="routeData">The route information from which we need the track database and the tsectiondat</param>
        /// <param name="pathEditor"></param>
        public void SetPathEditor(ORTS.TrackViewer.Drawing.RouteData routeData, PathEditor pathEditor)
        {
            this.routeData = routeData;
            this.pathEditor = pathEditor;
            this.pathEditor.ChangedPath += new ChangedPathHandler(OnPathChanged);

            if (ChartWindowIsOpen)
            {
                InitChartData();
                OnPathChanged(); // To make sure we have an initial update
            }
            else
            {
                pathData = null;
            }

        }

        /// <summary>
        /// Open the window for charting the path, and make sure its data is up-to-date
        /// </summary>
        public void Open()
        {
            if (pathEditor == null || pathEditor.currentTrainPath == null)
            {
                return;
            }
            if (pathData == null)
            {
                InitChartData();
            }
            OnPathChanged();
            if (pathData == null)
            {   // it path is broken, OnPathChanged performed a close
                return;
            }
            chartWindow.Show();

        }

        /// <summary>
        /// Close (Hide) the window with the charts and remove memory used for storing the path chart data
        /// </summary>
        public void Close()
        {
            chartWindow.Hide();
            pathData = null; // release memory, and do not do further update processing
        }

        /// <summary>
        /// Draw the dynamic parts of the chart (those parts that vary often).
        /// </summary>
        public void DrawDynamics()
        {

        }

        private void InitChartData()
        {
            this.pathData = new PathChartData(this.routeData);
            this.heightChart = new HeightChart(this.pathData);
            this.gradeChart = new GradeChart(this.pathData);
            this.curvatureChart = new CurvatureChart(this.pathData);
            chartWindow.SetCanvasData(0, this.heightChart);
            chartWindow.SetCanvasData(1, this.gradeChart);
            chartWindow.SetCanvasData(2, this.curvatureChart);
        }

        private void OnPathChanged()
        {
            if (pathData == null)
            {
                return;
            }

            Trainpath trainpath = pathEditor.currentTrainPath;
            pathData.Update(trainpath);
            chartWindow.Draw();
            chartWindow.SetTitle(pathEditor.currentTrainPath.PathName);
        }
    }
    #endregion

    interface ISubChart
    {
        void Draw(double zoomPercentageStart, double zoomPercentageStop, Canvas drawingCanvas);
    }

    #region SubChart
    /// <summary>
    /// Abstract base class for creating WPF charts for paths. 
    /// Contains a reference to the data as well as general scaling routines
    /// </summary>
    public abstract class SubChart : ISubChart
    {
        #region Properties
        /// <summary>The data for the path that is needed to create charts</summary>
        protected PathChartData pathData;

        /// <summary>Minimum of all x-values in this chart</summary>
        protected double minX;
        /// <summary>Maximum of all x-values in this chart</summary>
        protected double maxX;
        /// <summary>Minimum of all y-values in this chart</summary>
        protected double minY;
        /// <summary>Maximum of all y-values in this chart</summary>
        protected double maxY;

        /// <summary>Minimum of all x-values in the zoomed part of this chart</summary>
        protected double zoomedMinX;
        /// <summary>Maximum of all x-values in the zoomed part of this chart</summary>
        protected double zoomedMaxX;
        /// <summary>Minimum of all y-values in the zoomed part of this chart</summary>
        protected double zoomedMinY;
        /// <summary>Maximum of all y-values in the zoomed part of this chart</summary>
        protected double zoomedMaxY;

        /// <summary>Ratio (number between 0 and 1) of where the left-x-value is of the zoom area, compared to the total x-range</summary>
        protected double zoomRatioLeft;
        /// <summary>Ratio (number between 0 and 1) of where the right-x-value is of the zoom area, compared to the total x-range</summary>
        protected double zoomRatioRight;

        /// <summary>The width in pixels of the canvas we are drawing on</summary>
        protected double canvasWidth;
        /// <summary>The height in pixels of the canvas we are drawing on</summary>
        protected double canvasHeight;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="chartData">The data for which we will be creating charts</param>
        protected SubChart(PathChartData chartData)
        {
            this.pathData = chartData;
        }

        /// <summary>
        /// Draw the current chart on the given canvas. 
        /// This will do some general things, but the actual drawing for the specific chart is handed over to DrawSubChart.
        /// </summary>
        /// <param name="zoomRatioStart">Describing the start (left-side) of the zooming region (as a part of the total x-range)</param>
        /// <param name="zoomRatioStop">Describing the stop (right-side) of the zooming region (as a part of the total x-range)</param>
        /// <param name="drawingCanvas">The WPF canvas to draw upon</param>
        public void Draw(double zoomRatioStart, double zoomRatioStop, Canvas drawingCanvas)
        {
            drawingCanvas.Children.Clear();
            canvasWidth = drawingCanvas.ActualWidth;
            canvasHeight = drawingCanvas.ActualHeight;

            this.zoomRatioLeft = zoomRatioStart;
            this.zoomRatioRight = zoomRatioStop;

            DrawSubChart(drawingCanvas);
        }

        /// <summary>
        /// Abstract method to do the chart-specific (and hence not the general) parts of the drawing
        /// </summary>
        /// <param name="drawingCanvas">The WPF canvas to draw upon</param>
        abstract protected void DrawSubChart(Canvas drawingCanvas);

        #region General drawing routines
        /// <summary>
        /// Draw a simple line on a WPDthe canvas
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        /// <param name="x1">x-value of first point</param>
        /// <param name="y1">y-value of first point</param>
        /// <param name="x2">x-value of second point</param>
        /// <param name="y2">y-value of second point</param>
        /// <param name="color">Color of the line</param>
        protected static void DrawLine(Canvas drawingCanvas, double x1, double y1, double x2, double y2, Color color)
        {
            SolidColorBrush lineBrush = new SolidColorBrush(color);
            Line line = new Line();
            line.Stroke = lineBrush;
            line.StrokeThickness = 1;
            line.X1 = x1;
            line.X2 = x2;
            line.Y1 = y1;
            line.Y2 = y2;
            RenderOptions.SetEdgeMode(line, EdgeMode.Aliased);
            drawingCanvas.Children.Add(line);
        }

        /// <summary>
        /// Draw a poly line based on the stored data
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        /// <param name="getField">The method to get a value (normally a field) of a PathChartPoint, that is then used for drawing</param>
        /// <param name="repeatPreviousPoint">True if only vertical and horizontal lines are drawn (from one point to anohter).</param>
        protected void DrawDataPolyLine(Canvas drawingCanvas, Func<PathChartPoint, float> getField, bool repeatPreviousPoint)
        {
            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;

            Polyline dataPolyLine = new Polyline();
            dataPolyLine.Stroke = blackBrush;
            dataPolyLine.StrokeThickness = 1;

            var points = new PointCollection();
            double lastY = ScaledY(0d);
            foreach (PathChartPoint sourcePoint in this.pathData.PathChartPoints)
            {
                double newX = ScaledX(sourcePoint.DistanceAlongPath);
                double newY = ScaledY(getField(sourcePoint));

                if (newY != lastY && repeatPreviousPoint)
                {
                    points.Add(new Point(newX, lastY));
                }

                points.Add(new Point(newX, newY));
                lastY = newY;
            }

            dataPolyLine.Points = points;
            drawingCanvas.Children.Add(dataPolyLine);
        }

        /// <summary>
        /// Draw/print text on the WPF canvas
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        /// <param name="leftX">The x-position at the left of the text</param>
        /// <param name="centerY">The y-position at the center of the text</param>
        /// <param name="text">The text to draw/print</param>
        /// <param name="color">The color to use for printing the text</param>
        protected static void DrawText(Canvas drawingCanvas, double leftX, double centerY, string text, Color color)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(color);
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); // find out how tall it wants to be
            Canvas.SetLeft(textBlock, leftX);
            Canvas.SetTop(textBlock, centerY - textBlock.DesiredSize.Height / 2); // So no it should be centered vertically
            
            drawingCanvas.Children.Add(textBlock);
        }

        /// <summary>
        /// Draw/print text on the WPF canvas, rotated 90 degrees (so text is vertical) 
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        /// <param name="centerX">The x-postion at the center of the text</param>
        /// <param name="bottomY">The y-position at the bottom of the text (text will be drawn upwards)</param>
        /// <param name="text">The text to draw/print</param>
        /// <param name="color">The color to use for printing the text</param>
        protected static void DrawTextVertical(Canvas drawingCanvas, double centerX, double bottomY, string text, Color color)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(color);
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); // find out how tall it wants to be
            Canvas.SetLeft(textBlock, centerX - textBlock.DesiredSize.Height/2);
            Canvas.SetTop(textBlock, bottomY); 
            textBlock.RenderTransform = new RotateTransform(-90);

            drawingCanvas.Children.Add(textBlock);
        }

        /// <summary>
        /// Draw an image centered at the given location and identified by its name on the canvas
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        /// <param name="centerX">The x-position at the center of the image</param>
        /// <param name="centerY">The y-position at the center of the image</param>
        /// <param name="imageName">The name of the image</param>
        protected static void DrawImage(Canvas drawingCanvas, double centerX, double centerY, string imageName)
        {
            int imageSize = 10;
            var rect = new Rectangle()
            {
                Width = imageSize,
                Height = imageSize,
                Fill = new ImageBrush(BitmapImageManager.Instance.GetImage(imageName))
            };
            Canvas.SetLeft(rect, centerX-imageSize/2);
            Canvas.SetTop(rect, centerY-imageSize/2);
            drawingCanvas.Children.Add(rect);
        }
        #endregion

        #region Scaling
        /// <summary>
        /// Determine the scaling needed: store the minimum and maximum of the x and y values, both for the whole set of data as for the set of data visible after zooming
        /// </summary>
        /// <param name="getField">The method to get a value (normally a field) of a PathChartPoint, that is then used for drawing</param>
        protected void DetermineScaling(Func<PathChartPoint,float> getField)
        {
            this.minX = this.pathData.PointWithMinima.DistanceAlongPath;
            this.maxX = this.pathData.PointWithMaxima.DistanceAlongPath;
            this.minY = getField(this.pathData.PointWithMinima);
            this.maxY = getField(this.pathData.PointWithMaxima);

            this.zoomedMaxX = minX + (maxX - minX) * this.zoomRatioRight;
            this.zoomedMinX = minX + (maxX - minX) * this.zoomRatioLeft;

            DetermineZoomedRangeY(getField);
        }

        private void DetermineZoomedRangeY(Func<PathChartPoint, float> getField)
        {
            zoomedMinY = Double.MaxValue;
            zoomedMaxY = Double.MinValue;
            bool AddingPoints = true;
            foreach (PathChartPoint sourcePoint in this.pathData.PathChartPoints)
            {
                float currentY = getField(sourcePoint);

                // for the zoomed-Y we want to make sure that not only the points inside the zooming region, but also both points just outside
                if (sourcePoint.DistanceAlongPath < zoomedMinX)
                {
                    zoomedMinY = currentY;
                    zoomedMaxY = currentY;
                }
                else
                {
                    if (AddingPoints)
                    {
                        if (currentY > zoomedMaxY)
                        {
                            zoomedMaxY = currentY;
                        }
                        if (currentY < zoomedMinY)
                        {
                            zoomedMinY = currentY;
                        }
                    }
                    if (sourcePoint.DistanceAlongPath > zoomedMaxX)
                    {
                        AddingPoints = false;
                    }
                }
            }

            // To prevent the zoom range on the Y-value to become too small
            double minZoomedRange = (this.maxY - this.minY) / 10;
            if (this.zoomedMaxY - this.zoomedMinY < minZoomedRange)
            {
                double zoomedAverage = (this.zoomedMaxY + this.zoomedMinY) / 2;
                this.zoomedMinY = zoomedAverage - minZoomedRange / 2;
                this.zoomedMaxY = zoomedAverage + minZoomedRange / 2;
            }
        }

        /// <summary>
        /// Return the x-value in pixels from a x-value in the data
        /// </summary>
        /// <param name="sourceX">The x-value in the data</param>
        protected double ScaledX(double sourceX)
        {
            int rightMargin = 10;
            int leftMargin = 50;
            double effectiveWidth = canvasWidth - (leftMargin + rightMargin);
            return leftMargin + (sourceX - zoomedMinX) / (zoomedMaxX - zoomedMinX) * effectiveWidth;
        }

        /// <summary>
        /// Return the y-value in pixels from a y-value in the data
        /// </summary>
        /// <param name="sourceY">The y-value in the data</param>
        protected double ScaledY(double sourceY)
        {
            int topMargin = 10;
            int botMargin = 10;
            double effectiveHeight = canvasHeight - (botMargin + topMargin);
            return canvasHeight - botMargin - (sourceY - minY) / (maxY - minY) * effectiveHeight;
        }
        #endregion
    }
    #endregion

    #region HeightChart
    /// <summary>
    /// The chart that shows the height of the train path as function of the distance along the path.
    /// Also prints the grade in numbers and prints the station names
    /// </summary>
    public class HeightChart : SubChart
    {
        private NiceScaling niceScale;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pathChartData">The data for which we will be creating charts</param>
        public HeightChart(PathChartData pathChartData) : base(pathChartData) { }

        /// <summary>
        /// Overridden abstract method to do the chart-specific (and hence not the general) parts of the drawing
        /// </summary>
        /// <param name="drawingCanvas">The WPF canvas to draw upon</param>
        protected override void DrawSubChart(Canvas drawingCanvas)
        {
            DetermineScaling(p => p.HeightM);
            CleanScaling();
            DrawHorizontalGridLines(drawingCanvas);
            DrawDataPolyLine(drawingCanvas, p => p.HeightM, false);
            PrintGradesAndStations(drawingCanvas);
            ShowSpecialNodes(drawingCanvas);

        }

        /// <summary>
        /// Make sure the scaling and grid lines are nice (either in meters or feet, depending on user settings)
        /// </summary>
        private void CleanScaling()
        {
            if (Properties.Settings.Default.useMilesNotMeters)
            {
                niceScale = new NiceScaling(zoomedMinY, zoomedMaxY, Me.FromFt(1.0f), "ft");
            }
            else
            {
                niceScale = new NiceScaling(zoomedMinY, zoomedMaxY, 1.0, "m");
            }
            minY = (double)niceScale.ValueMin;
            maxY = (double)niceScale.ValueMax;
        }

        private void DrawHorizontalGridLines(Canvas drawingCanvas)
        {
            foreach (decimal niceValue in niceScale.NiceValues)
            {
                DrawLine(drawingCanvas, ScaledX(this.zoomedMinX), ScaledY((double)niceValue), ScaledX(this.zoomedMaxX), ScaledY((double)niceValue), Colors.Gray);
                DrawHeightLabel(drawingCanvas, niceValue);
            }
        }

        /// <summary>
        /// Draw a single y-axis label for the height
        /// </summary>
        /// <param name="drawingCanvas">The WPF canvas to draw upon</param>
        /// <param name="height">The height for this label</param>
        private void DrawHeightLabel(Canvas drawingCanvas, decimal height)
        {
            string textToDraw = String.Format("{0}{1}", height/niceScale.Scale, niceScale.Unit);
            DrawText(drawingCanvas, 5, ScaledY((double)height), textToDraw, Colors.Black);
        }

        /// <summary>
        /// Print the grades (in percent) and station names along the chart.
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        private void PrintGradesAndStations(Canvas drawingCanvas)
        {
            double lastX = ScaledX(0);
            string lastTextToDraw = String.Format("{0,5:0.0}", 0);

            foreach (PathChartPoint sourcePoint in this.pathData.PathChartPoints)
            {
                double newX = ScaledX(sourcePoint.DistanceAlongPath);
                double newY = sourcePoint.GradePercent;
                string newTextToDraw = String.Format("{0,5:0.0}", newY);

                if (lastTextToDraw != newTextToDraw)
                {
                    DrawTextVertical(drawingCanvas, (lastX+newX)/2, ScaledY(this.minY), lastTextToDraw, Colors.Black);
                    lastX = newX;
                    lastTextToDraw = newTextToDraw;
                }

                if (sourcePoint.StationName != null)
                {
                    DrawTextVertical(drawingCanvas, newX, 100, sourcePoint.StationName, Colors.Red);

                }
            }
        }

        private void ShowSpecialNodes(Canvas drawingCanvas)
        {
            foreach (KeyValuePair<TrainpathNode,double> item in this.pathData.DistanceAlongPath)
            {
                if (item.Key.NodeType == TrainpathNodeType.Reverse)
                {
                    
                    DrawImage(drawingCanvas, ScaledX(item.Value), ScaledY(item.Key.Location.Location.Y), "pathReverse");
                }
                if (item.Key.NodeType == TrainpathNodeType.Stop)
                {

                    DrawImage(drawingCanvas, ScaledX(item.Value), ScaledY(item.Key.Location.Location.Y), "pathWait");
                }
                if (item.Key.IsBroken)
                {

                    DrawImage(drawingCanvas, ScaledX(item.Value), ScaledY(item.Key.Location.Location.Y), "activeBroken");
                }
            }
        }

    }
    #endregion

    #region GradeChart
    /// <summary>
    /// The chart that shows the grade of the path (in percentages)
    /// Also prints the text of the x-location
    /// </summary>
    public class GradeChart: SubChart
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pathChartData">The data for which we will be creating charts</param>
        public GradeChart(PathChartData pathChartData) : base(pathChartData) { }

        /// <summary>
        /// Overridden abstract method to do the chart-specific (and hence not the general) parts of the drawing
        /// </summary>
        /// <param name="drawingCanvas">The WPF canvas to draw upon</param>
        protected override void DrawSubChart(Canvas drawingCanvas)
        {
            DetermineScaling(p => p.GradePercent);
            CleanScaling();
            
            DrawHorizontalGridLines(drawingCanvas);

            DrawDataPolyLine(drawingCanvas, p => p.GradePercent, true);
            PrintHorizontalScaleLabels(drawingCanvas);

        }

        private void DrawHorizontalGridLines(Canvas drawingCanvas)
        {
            DrawLine(drawingCanvas, ScaledX(this.zoomedMinX), ScaledY(0d), ScaledX(this.zoomedMaxX), ScaledY(0d), Colors.Black);
            DrawGradePercentageLabel(drawingCanvas, 0);

            for (int grade = 1; grade <= maxY; grade++)
            {
                DrawLine(drawingCanvas, ScaledX(this.zoomedMinX), ScaledY(grade), ScaledX(this.zoomedMaxX), ScaledY(grade), Colors.Gray);
                DrawGradePercentageLabel(drawingCanvas, grade);
            }

            for (int grade = 1; grade <= Math.Abs(minY); grade++)
            {
                DrawLine(drawingCanvas, ScaledX(this.zoomedMinX), ScaledY(-grade), ScaledX(this.zoomedMaxX), ScaledY(-grade), Colors.Gray);
                DrawGradePercentageLabel(drawingCanvas, -grade);
            }
        }

        /// <summary>
        /// Draw a y-axis label for the grade 
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        /// <param name="grade">The grade value for which to draw the label</param>
        private void DrawGradePercentageLabel(Canvas drawingCanvas, int grade)
        {
            string textToDraw = String.Format("{0}%", grade);
            DrawText(drawingCanvas, 5, ScaledY(grade), textToDraw, Colors.Black);
        }

        /// <summary>
        /// The auto-scaling is already done. Here we would like to make sure that at least '0' is part of the scale
        /// </summary>
        private void CleanScaling()
        {
            if (minY >= 0)
            {// make sure we always have something below 0
                minY = -0.5;
            }
            else
            {
                minY = Math.Floor(minY);
            }

            if (maxY <= 0)
            {   // make sure we always have something above 0
                maxY = 0.5;
            }
            else
            {
                maxY = Math.Ceiling(maxY);
            }
        }

        /// <summary>
        /// Print the x-axes labels
        /// </summary>
        /// <param name="drawingCanvas">The canvas to draw upon</param>
        private void PrintHorizontalScaleLabels(Canvas drawingCanvas)
        {
            NiceScaling niceScale;
            int minNumberOfTicks = Math.Max(4, (int)(this.canvasWidth/100));
        
            if (Properties.Settings.Default.useMilesNotMeters)
            {
                niceScale = new NiceScaling(this.zoomedMinX, this.zoomedMaxX, Me.FromMi(1.0f), "M", minNumberOfTicks, true);
            }
            else
            {
                niceScale = new NiceScaling(this.zoomedMinX, this.zoomedMaxX, Me.FromKiloM(1.0f), "km", minNumberOfTicks, true);
            }

            foreach (decimal niceValue in niceScale.NiceValues)
            {
                string textToDraw = String.Format("{0}{1}", niceValue / niceScale.Scale, niceScale.Unit);
                DrawText(drawingCanvas, ScaledX((double)niceValue), 5, textToDraw, Colors.Black);
                //DrawTextVertical(drawingCanvas, ScaledX(niceValue), 5, textToDraw, Colors.Red);

            }
        }
    }
    #endregion

    #region CurvatureChart
    /// <summary>
    /// Chart that shows the curvature of the track along the train path.
    /// </summary>
    public class CurvatureChart : SubChart
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pathChartData">The data for which we will be creating charts</param>
        public CurvatureChart(PathChartData pathChartData) : base(pathChartData) { }

        /// <summary>
        /// Overridden abstract method to do the chart-specific (and hence not the general) parts of the drawing
        /// </summary>
        /// <param name="drawingCanvas">The WPF canvas to draw upon</param>
        protected override void DrawSubChart(Canvas drawingCanvas)
        {
            DetermineScaling(CurvatureDeviation);
            DrawDataPolyLine(drawingCanvas, CurvatureDeviation, true);
            DebugShowDrawTime(drawingCanvas);
        }

        private void DebugShowDrawTime(Canvas drawingCanvas)
        {
            if (!System.Diagnostics.Debugger.IsAttached) return;
            DrawText(drawingCanvas, 10, ScaledY(0), DateTime.Now.ToString("mm:ss"), Colors.Black);
        }

        /// <summary>
        /// Translate a curvature to a deviation in the chart. For now, basically we use sign
        /// </summary>
        /// <param name="pathPoint">The single point for which to determine the deviation from 0</param>
        private float CurvatureDeviation(PathChartPoint pathPoint)
        {
            float deviation =  10 * Math.Sign(pathPoint.Curvature);
            //deviation = pathPoint.Curvature;
            return deviation;
        }
    }
    #endregion

    #region NiceScaling
    /// <summary>
    /// Class to make it easier to do nice scaling of charts.
    /// This means making sure the scales on the x or y axis are nice round numbers
    /// We must make sure at least 4 different sub-divisions are present. The scheme below makes sure it is never more than 8
    /// sub-division min-range
    /// 1          4
    /// 2          8
    /// 4         16
    /// 5         20
    ///10         40 
    ///
    /// It is also possible to get nice values in a different scale (and unit) then the original data.
    /// If the original data is in meters, but you want to scaling to be in feet, specify the scale to be 0.3048 and the unit in 'ft' or so.
    /// </summary>
    public class NiceScaling
    {
        /// <summary>
        /// List of nice round equidistant values covering the range of interest. Values are given in original units (so not scaled)
        /// but they are round in scaled units.
        /// </summary>
        public IEnumerable<decimal> NiceValues { get; private set; }
        /// <summary>Minimum of the NiceValues</summary>
        public decimal ValueMin { get; private set; }
        /// <summary>Maximum of the NiceValues</summary>
        public decimal ValueMax { get; private set; }
        /// <summary>Scale between the unit of the data and the unit used for presenting (e.g. 0.3048 for data in m but presenting in feet)</summary>
        public decimal Scale { get; private set; }
        /// <summary>Unit to be used for printing</summary>
        public string Unit { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="valueMin">Minimum value for which a nice scale is needed</param>
        /// <param name="valueMax">Maximum value for which a nice scale is needed</param>
        /// <param name="scale">The requested scale Factor.</param>
        /// <param name="unit">The string to use as unit</param>
        /// <param name="minNumberOfTicks">optional: The minimum number of nice ticks</param>
        /// <param name="strict">When set, the nice values will never exceed the given ranges</param>
        public NiceScaling(double valueMin, double valueMax, double scale, string unit, int minNumberOfTicks=4, bool strict=false)
        {
            if (valueMin == valueMax)
            {
                // exceptional situation, there is no range. So make one up.
                if (valueMin == 0)
                {
                    valueMin = -1;
                    valueMax = +1;
                }
                else
                {
                    if (valueMin < 0)
                    {
                        valueMax = 0;
                    }
                    else
                    {
                        valueMin = 0;
                    }
                }
            }


            this.Unit = unit;
            double valueScaledMin = valueMin/scale;
            double valueScaledMax = valueMax/scale;
            double subDivisionMax = (valueScaledMax - valueScaledMin)/minNumberOfTicks;
            double sub10log = Math.Floor(Math.Log10(subDivisionMax));

            double power10 = Math.Pow(10.0, sub10log);
            double subDivisionMaxNice = subDivisionMax / power10;

            //Now we move to decimal to make sure we do not have rounding errors
            this.Scale = (decimal)scale;
            decimal valueScaledStep = ((decimal)power10) * (
                (subDivisionMaxNice >= 5) ? 5 :
                (subDivisionMaxNice >= 4) ? 4 :
                (subDivisionMaxNice >= 2) ? 2 :
                1);
            decimal valueScaledStart = valueScaledStep * (Math.Floor(((decimal)valueScaledMin) / valueScaledStep));
            if (strict && (valueScaledStart < (decimal)valueScaledMin))
            {
                valueScaledStart += valueScaledStep;
            }
            
            List<decimal> values = new List<decimal>();
            decimal valueScaled = valueScaledStart;
            while (valueScaled < (decimal)valueScaledMax) {
                values.Add(valueScaled * this.Scale);
                valueScaled += valueScaledStep;
            }
            values.Add(valueScaled * this.Scale);

            NiceValues = values.ToArray();
            ValueMin = valueScaledStart * this.Scale;
            ValueMax = valueScaled      * this.Scale;
        }
    }
    #endregion
}
