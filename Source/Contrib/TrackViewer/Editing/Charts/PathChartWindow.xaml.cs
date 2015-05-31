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
        private ISubChart[] subCharts;
        #region Construction and data setting
        /// <summary>
        /// The Window where the details of a path can be charted (height, grade, ...)
        /// </summary>
        public PathChartWindow()
        {
            InitializeComponent();
            subCharts = new ISubChart[3];
        }

        internal void SetCanvasData(int canvasIndex, ISubChart subChart)
        {
            if (canvasIndex >= 0 && canvasIndex < subCharts.Count())
            {
                subCharts[canvasIndex] = subChart;
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
            if (subCharts[0] != null) 
            {
                subCharts[0].Draw(zoomRatioStart, zoomRatioStop, HeightCanvas);
                subCharts[1].Draw(zoomRatioStart, zoomRatioStop, GradeCanvas);
                subCharts[2].Draw(zoomRatioStart, zoomRatioStop, CurvatureCanvas);
            }
            if (HeightCanvas.ActualWidth > 80)
            {
                ChartScrollbar.Width = HeightCanvas.ActualWidth - 80;
            }
            else
            {
                ChartScrollbar.Width = 800;
            }

        }

        /// <summary>
        /// Set the title of the window
        /// </summary>
        /// <param name="newTitle"></param>
        public void SetTitle(string newTitle)
        {
            this.Title = newTitle;
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
        #endregion

        #region Zooming
        private void ZoomIncrease_Click(object sender, RoutedEventArgs e)
        {
            ZoomChange(1.5);
        }

        private void ZoomDecrease_Click(object sender, RoutedEventArgs e)
        {
            ZoomChange(1.0/1.5);
        }

        private void ZoomChange(double zoomFactor)
        {
            double zoomCenter = ChartScrollbar.Value + ChartScrollbar.ViewportSize / 2;
            ChartScrollbar.ViewportSize = ChartScrollbar.ViewportSize / zoomFactor;
            if (ChartScrollbar.ViewportSize > 0.9999)
            {
                ChartScrollbar.ViewportSize = 0.9999;
            }
            ChartScrollbar.Maximum = 1 - ChartScrollbar.ViewportSize;
            ChartScrollbar.Value = zoomCenter - ChartScrollbar.ViewportSize / 2;
            //Draw(); // No draw is needed here, the Draw will be triggered by a change in value
        }
        #endregion

        private void ChartScrollbar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Draw();
        }

    }
}
