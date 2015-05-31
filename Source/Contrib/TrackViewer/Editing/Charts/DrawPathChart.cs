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

namespace ORTS.TrackViewer.Editing.Charts
{
    /// <summary>
    /// Wrapper class for drawing charts of paths. This class make sure the data is updated, and also the actual window is shown or closed as needed
    /// </summary>
    class DrawPathChart
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
            if (trainpath.IsBroken)
            {
                MessageBox.Show(
                    TrackViewer.catalog.GetString("For broken paths charting is not supported"));

                Close();
                return;
            }

            pathData.Update(trainpath);
            chartWindow.Draw();
            chartWindow.SetTitle(pathEditor.currentTrainPath.PathName);
        }
    }

    interface ISubChart
    {
        void Draw(Canvas drawingCanvas);
    }

    public abstract class SubChart : ISubChart
    {
        protected PathChartData pathData;

        protected double minX;
        protected double maxX;
        protected double minY;
        protected double maxY;

        protected double canvasWidth;
        protected double canvasHeight;

        public void Draw(Canvas drawingCanvas)
        {
            drawingCanvas.Children.Clear();
            canvasWidth = drawingCanvas.ActualWidth;
            canvasHeight = drawingCanvas.ActualHeight;

            DrawSubChart(drawingCanvas);
        }

        abstract protected void DrawSubChart(Canvas drawingCanvas);

        #region General drawing routines
        protected void DrawLine(Canvas drawingCanvas, double x1, double y1, double x2, double y2, Color color)
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

        protected void DrawDataPolyLine(Canvas drawingCanvas, IEnumerable<PathChartPoint> sourcePoints, Func<PathChartPoint, float> getField, bool repeatPreviousPoint)
        {
            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;

            Polyline dataPolyLine = new Polyline();
            dataPolyLine.Stroke = blackBrush;
            dataPolyLine.StrokeThickness = 1;

            var points = new PointCollection();
            double lastX = ScaledX(0);
            double lastY = ScaledY(0);
            foreach (PathChartPoint sourcePoint in sourcePoints)
            {
                double newX = ScaledX(sourcePoint.DistanceAlongPath);
                double newY = ScaledY(getField(sourcePoint));

                if (newY != lastY && repeatPreviousPoint)
                {
                    points.Add(new Point(lastX, newY));
                }

                points.Add(new Point(newX, newY));
                lastX = newX;
                lastY = newY;
            }

            //var points = new PointCollection();
            //foreach (PathChartPoint sourcePoint in sourcePoints)
            //{
            //    points.Add(new Point(ScaledX(sourcePoint.DistanceAlongPath), ScaledY(getField(sourcePoint))));
            //}

            dataPolyLine.Points = points;
            drawingCanvas.Children.Add(dataPolyLine);
        }

        protected void DrawText(Canvas drawingCanvas, double leftX, double centerY, string text, Color color)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(color);
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); // find out how tall it wants to be
            Canvas.SetLeft(textBlock, leftX);
            Canvas.SetTop(textBlock, centerY - textBlock.DesiredSize.Height / 2); // So no it should be centered vertically
            
            drawingCanvas.Children.Add(textBlock);
        }

        protected void DrawTextVertical(Canvas drawingCanvas, double centerX, double bottomY, string text, Color color)
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
        #endregion

        #region Scaling
        protected void DetermineScaling(IEnumerable<PathChartPoint> points, Func<PathChartPoint,float> getField)
        {
            //Find min and max values
            minY = Double.MaxValue;
            maxY = Double.MinValue;
            minX = 0;
            maxX = 0;
            foreach (PathChartPoint sourcePoint in points)
            {
                float currentY = getField(sourcePoint);
                maxX = sourcePoint.DistanceAlongPath;
                if (currentY > maxY)
                {
                    maxY = currentY;
                }
                if (currentY < minY)
                {
                    minY = currentY;
                }
            }
        }

        protected void DetermineScalingOld(PointCollection points)
        {
            //Find min and max values
            minY = Double.MaxValue;
            maxY = Double.MinValue;
            minX = 0;
            maxX = 0;
            foreach (Point sourcePoint in points)
            {
                maxX = sourcePoint.X;
                if (sourcePoint.Y > maxY)
                {
                    maxY = sourcePoint.Y;
                }
                if (sourcePoint.Y < minY)
                {
                    minY = sourcePoint.Y;
                }
            }
        }

        protected double ScaledX(double sourceX)
        {
            int rightMargin = 10;
            int leftMargin = 50;
            double effectiveWidth = canvasWidth - (leftMargin + rightMargin);
            return leftMargin + (sourceX - minX) / (maxX - minX) * effectiveWidth;
        }

        protected double ScaledY(double sourceY)
        {
            int topMargin = 10;
            int botMargin = 10;
            double effectiveHeight = canvasHeight - (botMargin + topMargin);
            return canvasHeight - botMargin - (sourceY - minY) / (maxY - minY) * effectiveHeight;
        }

        protected Point ScaledPoint(Point sourcePoint)
        {
            return new Point(ScaledX(sourcePoint.X), ScaledY(sourcePoint.Y));
        }
        #endregion
    }

    public class HeightChart : SubChart
    {
        private NiceScaling niceScale;

        public HeightChart(PathChartData pathChartData)
        {
            this.pathData = pathChartData;
        }

        protected override void DrawSubChart(Canvas drawingCanvas)
        {
            DetermineScaling(this.pathData.PathChartPoints, p => p.HeightM);
            CleanScaling();
            DrawHorizontalLines(drawingCanvas);
            DrawDataPolyLine(drawingCanvas, this.pathData.PathChartPoints, p => p.HeightM, false);
            PrintGrades(drawingCanvas);
        }

        private void CleanScaling()
        {
            if (Properties.Settings.Default.useMilesNotMeters)
            {
                niceScale = new NiceScaling(minY, maxY, Me.FromFt(1.0f), "ft");
            }
            else
            {
                niceScale = new NiceScaling(minY, maxY, 1.0, "m");
            }
            minY = niceScale.ValueMin;
            maxY = niceScale.ValueMax;
        }

        private void DrawHorizontalLines(Canvas drawingCanvas)
        {
            foreach (double niceValue in niceScale.NiceValues)
            {
                DrawLine(drawingCanvas, ScaledX(0), ScaledY(niceValue), ScaledX(maxX), ScaledY(niceValue), Colors.Gray);
                DrawHeightText(drawingCanvas, niceValue);
            }
        }

        private void DrawHeightText(Canvas drawingCanvas, double height)
        {
            string textToDraw = String.Format("{0}{1}", height/niceScale.Scale, niceScale.Unit);
            DrawText(drawingCanvas, 5, ScaledY(height), textToDraw, Colors.Black);
        }

        private void PrintGrades(Canvas drawingCanvas)
        {
            double lastX = ScaledX(0);
            double startX = lastX;
            string lastTextToDraw = String.Format("{0,5:0.0}", 0);

            foreach (PathChartPoint sourcePoint in this.pathData.PathChartPoints)
            {
                double newX = ScaledX(sourcePoint.DistanceAlongPath);
                double newY = sourcePoint.GradePercent;
                string newTextToDraw = String.Format("{0,5:0.0}", newY);

                if (lastTextToDraw != newTextToDraw)
                {
                    DrawTextVertical(drawingCanvas, (lastX+startX)/2, ScaledY(this.minY), lastTextToDraw, Colors.Black);
                    startX = lastX;
                    lastTextToDraw = newTextToDraw;
                }
                lastX = newX;
            }
        }
    }

    class GradeChart: SubChart
    {
        

        public GradeChart(PathChartData pathChartData)
        {
            this.pathData = pathChartData;
        }

        protected override void DrawSubChart(Canvas drawingCanvas)
        {
            DetermineScaling(this.pathData.PathChartPoints, p => p.GradePercent);
            CleanScaling();
            
            DrawHorizontalLines(drawingCanvas);

            DrawDataPolyLine(drawingCanvas, this.pathData.PathChartPoints, p => p.GradePercent, true);
            HorizontalScaleText(drawingCanvas);

        }

        private void DrawHorizontalLines(Canvas drawingCanvas)
        {
            DrawLine(drawingCanvas, ScaledX(0), ScaledY(0), ScaledX(maxX), ScaledY(0), Colors.Black);
            DrawGradePercentageText(drawingCanvas, 0);

            for (int grade = 1; grade <= maxY; grade++)
            {
                DrawLine(drawingCanvas, ScaledX(0), ScaledY(grade), ScaledX(maxX), ScaledY(grade), Colors.Gray);
                DrawGradePercentageText(drawingCanvas, grade);
            }

            for (int grade = 1; grade <= Math.Abs(minY); grade++)
            {
                DrawLine(drawingCanvas, ScaledX(0), ScaledY(-grade), ScaledX(maxX), ScaledY(-grade), Colors.Gray);
                DrawGradePercentageText(drawingCanvas, -grade);
            }
        }

        private void DrawGradePercentageText(Canvas drawingCanvas, int grade)
        {
            string textToDraw = String.Format("{0}%", grade);
            DrawText(drawingCanvas, 5, ScaledY(grade), textToDraw, Colors.Black);
        }

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

        private void HorizontalScaleText(Canvas drawingCanvas)
        {
            NiceScaling niceScale;
        
            if (Properties.Settings.Default.useMilesNotMeters)
            {
                niceScale = new NiceScaling(minX, maxX, Me.FromMi(1.0f), "M");
            }
            else
            {
                niceScale = new NiceScaling(minX, maxX, Me.FromKiloM(1.0f), "km");
            }

            foreach (double niceValue in niceScale.NiceValues)
            {
                string textToDraw = String.Format("{0}{1}", niceValue / niceScale.Scale, niceScale.Unit);
                DrawText(drawingCanvas, ScaledX(niceValue), 5, textToDraw, Colors.Black);
                //DrawTextVertical(drawingCanvas, ScaledX(niceValue), 5, textToDraw, Colors.Red);

            }
        }
    }

    class CurvatureChart : SubChart
    {
        public CurvatureChart(PathChartData pathChartData)
        {
            this.pathData = pathChartData;
        }

        protected override void DrawSubChart(Canvas drawingCanvas)
        {
            DetermineScaling(this.pathData.PathChartPoints, CurvatureDeviation);
            DrawDataPolyLine(drawingCanvas, this.pathData.PathChartPoints, CurvatureDeviation, true);
        }

        private float CurvatureDeviation(PathChartPoint pathPoint)
        {
            float deviation =  10 * Math.Sign(pathPoint.Curvature);
            //deviation = pathPoint.Curvature;
            return deviation;
        }

        protected void DrawSpecialPolyLine(Canvas drawingCanvas, IEnumerable<PathChartPoint> sourcePoints, Func<PathChartPoint, float> getField)
        {
            SolidColorBrush blackBrush = new SolidColorBrush();
            blackBrush.Color = Colors.Black;

            Polyline dataPolyLine = new Polyline();
            dataPolyLine.Stroke = blackBrush;
            dataPolyLine.StrokeThickness = 1;

            var points = new PointCollection();
            double lastX = ScaledX(0);
            double lastY = ScaledY(0);
            foreach (PathChartPoint sourcePoint in sourcePoints)
            {
                double newX = ScaledX(sourcePoint.DistanceAlongPath);
                double newY = ScaledY(getField(sourcePoint));

                if (newY != lastY)
                {
                    points.Add(new Point(lastX, newY)); 
                }

                points.Add(new Point(newX, newY));
                lastX = newX; 
                lastY = newY;
            }

            dataPolyLine.Points = points;
            drawingCanvas.Children.Add(dataPolyLine);
        }
    }

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
    class NiceScaling
    {
        /// <summary>
        /// List of nice round equidistant values covering the range of interest. Values are given in original units (so not scaled)
        /// but they are round in scaled units.
        /// </summary>
        public IEnumerable<double> NiceValues { get; private set; }
        /// <summary>Minimum of the NiceValues</summary>
        public double ValueMin { get; private set; }
        /// <summary>Maximum of the NiceValues</summary>
        public double ValueMax { get; private set; }
        /// <summary>Scale between the unit of the data and the unit used for presenting (e.g. 0.3048 for data in m but presenting in feet)</summary>
        public double Scale { get; private set; }
        /// <summary>Unit to be used for printing</summary>
        public string Unit { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="valueMin">Minimum value for which a nice scale is needed</param>
        /// <param name="valueMax">Maximum value for which a nice scale is needed</param>
        /// <param name="scale">The requested scale Factor.</param>
        /// <param name="unit"></param>
        public NiceScaling(double valueMin, double valueMax, double scale, string unit)
        {
            if (valueMin == valueMax)
            {
                // exceptional situation, no range. So make one up.
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


            this.Scale = scale;
            this.Unit = unit;
            double valueScaledMin = valueMin/scale;
            double valueScaledMax = valueMax/scale;
            double subDivisionMax = (valueScaledMax - valueScaledMin)/4;
            double sub10log = Math.Floor(Math.Log10(subDivisionMax));
            double power10 = Math.Pow(10.0, sub10log);
            double subDivisionMaxNice = subDivisionMax / power10;
            double valueScaledStep = power10 * (
                (subDivisionMaxNice >= 5) ? 5 :
                (subDivisionMaxNice >= 4) ? 4 :
                (subDivisionMaxNice >= 2) ? 2 :
                1);
            double valueScaledStart = valueScaledStep * (Math.Floor(valueScaledMin / valueScaledStep));
            
            List<double> values = new List<double>();
            double valueScaled = valueScaledStart;
            while (valueScaled < valueScaledMax) {
                values.Add(valueScaled * scale);
                valueScaled += valueScaledStep;
            }
            values.Add(valueScaled * scale);

            NiceValues = values.ToArray();
            ValueMin = valueScaledStart * scale;
            ValueMax = valueScaled      * scale;

        }

    }
}
