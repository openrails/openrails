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

namespace ORTS.TrackViewer.Editing.Charts
{
    /// <summary>
    /// Interaction logic for PathChartWindow.xaml
    /// </summary>
    partial class PathChartWindow : Window
    {
        /// <summary>Does the window have focus (actived) or not</summary>
        public bool IsActivated { get; set; }

        private Dictionary<string, Canvas> chartCanvasses;
        private Dictionary<string, Canvas> legendCanvasses;
        private Dictionary<string, ISubChart> subCharts;

        #region Construction and data setting
        /// <summary>
        /// The Window where the details of a path can be charted (height, grade, ...)
        /// </summary>
        public PathChartWindow()
        {
            InitializeComponent();
            
            chartCanvasses = new Dictionary<string, Canvas> {
                {"height", HeightCanvas},
                {"grade", GradeCanvas},
                {"curvature", CurvatureCanvas},
                {"distance", DistanceCanvas},
                {"milemarkers", MileMarkersCanvas},
                {"speedmarkers", SpeedMarkersCanvas}
            };
            legendCanvasses = new Dictionary<string, Canvas> {
                {"height", HeightLegend},
                {"grade", GradeLegend},
                {"curvature", CurvatureLegend},
                {"distance", DistanceLegend},
                {"milemarkers", MileMarkersLegend},
                {"speedmarkers", SpeedMarkersLegend}
            };
            subCharts = new Dictionary<string, ISubChart>();
            ChartScrollbar.Value = ChartScrollbar.Maximum / 2;
        }

        internal void SetCanvasData(string canvasName, ISubChart subChart)
        {
            if (chartCanvasses.ContainsKey(canvasName))
            {
                subCharts[canvasName] = subChart;
            }
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw all the subcharts
        /// </summary>
        public void Draw()
        {
            //if (this.Visibility == Visibility.Visible && subCharts[0] != null) 
            double zoomRatioStart = ChartScrollbar.Value;
            double zoomRatioStop = ChartScrollbar.Value + ChartScrollbar.ViewportSize;
            foreach (string chartName in chartCanvasses.Keys)
            {
                ISubChart subChart;
                subCharts.TryGetValue(chartName, out subChart);
                if (subChart != null) {
                    subChart.Draw(zoomRatioStart, zoomRatioStop, chartCanvasses[chartName], legendCanvasses[chartName]);
                }
            }

            if (HeightCanvas.ActualWidth > 10)
            {
                ChartScrollbar.Width = HeightCanvas.ActualWidth - 10;
            }
            else
            {   // happens during initialization, when actualWidht is not yet non-zero
                ChartScrollbar.Width = 800;
            }

        }
        #endregion

        #region Window events
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // I tried to implement http://stackoverflow.com/questions/2046756/wpf-resize-complete
            // That method prevents redrawing multiple times during a resize. That is in principle better
            // But it did not work. For redrawing I need the new sizes (actualWidth, actualHeight).
            // Somehow the new sizes are not available to the thread dealing with the timer.
            // I have no idea why. But I am taking the redrawing hit for now.
            Draw();
        }

        /// <summary>
        /// We do not want to really close the window, but only hide it
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;  // cancels the window close    
            this.Hide();      // Programmatically hides the window
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.IsActivated = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.IsActivated = false;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this.IsActivated)
            {   // We want to make sure that mouse scrolling is not captured here if already captured in trackViewer.
                double xPositionOfMouse = Mouse.GetPosition(this.HeightCanvas).X;
                double xPositionOfMouseAsRatio = xPositionOfMouse / this.HeightCanvas.ActualWidth;

                ZoomChange(Math.Exp(0.1 * e.Delta / 40), xPositionOfMouseAsRatio);
            }
        }

        private bool mouseIsMoving = false;
        private double realXatMouse=0;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            var sourceElement = e.Source as Window;
            if (sourceElement != null)
            {
                sourceElement.CaptureMouse();
                this.mouseIsMoving = true;
                double mouseXstart = e.GetPosition(sourceElement).X;
                double mouseXAsRatio = mouseXstart / this.HeightCanvas.ActualWidth;
                this.realXatMouse = ChartScrollbar.Value + mouseXAsRatio * ChartScrollbar.ViewportSize;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.mouseIsMoving)
            {
                double mouseXstart = e.GetPosition(null).X;
                double mouseXAsRatio = mouseXstart / this.HeightCanvas.ActualWidth;
                ChartScrollbar.Value = this.realXatMouse - mouseXAsRatio * ChartScrollbar.ViewportSize;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var sourceElement = e.Source as Window;
            if (sourceElement != null)
            {
                sourceElement.ReleaseMouseCapture();
                this.mouseIsMoving = false;
            }
        }
        #endregion

        #region Zooming
        /// <summary>
        /// Zoom the charting window
        /// </summary>
        /// <param name="zoomFactor">Factor to zoom with</param>
        public void ZoomChange(double zoomFactor)
        {
            ZoomChange(zoomFactor, ChartScrollbar.Value / ChartScrollbar.Maximum);
        }

        private void ZoomChange(double zoomFactor, double zoomCenterRatio)
        {
            double oldValue = ChartScrollbar.Value;

            double zoomCenter = ChartScrollbar.Value + zoomCenterRatio * ChartScrollbar.ViewportSize;
            ChartScrollbar.ViewportSize = ChartScrollbar.ViewportSize / zoomFactor;
            if (ChartScrollbar.ViewportSize > 0.9999)
            {
                ChartScrollbar.ViewportSize = 0.9999;
            }
            ChartScrollbar.Maximum = 1 - ChartScrollbar.ViewportSize;
            ChartScrollbar.Value = zoomCenter - zoomCenterRatio * ChartScrollbar.ViewportSize;
            if (ChartScrollbar.Value == oldValue)
            {
                // Normally, a value change will trigger a draw. But in case value is 0, it stays at 0. So force a draw
                Draw();
            }
        }

        /// <summary>
        /// Shift the chartwindow (when zoomed)
        /// </summary>
        /// <param name="shiftSteps">The number of zoom steps (negative for shifting left)</param>
        public void Shift(int shiftSteps)
        {
            double shiftSize = Math.Min(ChartScrollbar.Maximum / 10, ChartScrollbar.ViewportSize * 0.5);
            ChartScrollbar.Value += shiftSteps * shiftSize;
            if (ChartScrollbar.Value < ChartScrollbar.Minimum) ChartScrollbar.Value = ChartScrollbar.Minimum;
            if (ChartScrollbar.Value > ChartScrollbar.Maximum) ChartScrollbar.Value = ChartScrollbar.Maximum;
        }

        private void ChartScrollbar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Draw();
        }
        #endregion

        #region Save JSON
        /// <summary>
        /// Action that can be set to act as a callback when the save .json button is clicked
        /// </summary>
        public Action OnJsonSaveClick { get; set; }

        private void SaveJson_Click(object sender, RoutedEventArgs e)
        {
            OnJsonSaveClick?.Invoke();
        }
        #endregion
    }
}
