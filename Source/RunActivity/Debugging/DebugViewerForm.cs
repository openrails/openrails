using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MSTS;

namespace ORTS.Debugging
{


   /// <summary>
   /// Defines a debugging viewer for use as an external window
   /// when using Open Rails 
   /// </summary>
   public partial class DebugViewerForm : Form
   {
      /// <summary>
      /// Reference to the main simulator object.
      /// </summary>
      private readonly Simulator simulator;

      /// <summary>
      /// Reference to the Viewer3D object.
      /// </summary>
      private readonly Viewer3D viewer;


      private int IM_Width = 512;
      private int IM_Height = 512;

      /// <summary>
      /// True when the user has the "Move left" pressed.
      /// </summary>
      private bool LeftButtonDown = false;

      /// <summary>
      /// True when the user has the "Move right" pressed.
      /// </summary>
      private bool RightButtonDown = false;

      /// <summary>
      /// True when the user has the "Move up" pressed.
      /// </summary>
      private bool UpButtonDown = false;

      /// <summary>
      /// True when the user has the "Move down" pressed.
      /// </summary>
      private bool DownButtonDown = false;


       /// <summary>
      /// Defines the area to view, in meters.
      /// </summary>
      private RectangleF ViewWindow;

      /// <summary>
      /// Used to periodically check if we should shift the view when the
      /// user is holding down a "shift view" button.
      /// </summary>
      private Timer UITimer;

      /// <summary>
      /// Creates a new DebugViewerForm.
      /// </summary>
      /// <param name="simulator"></param>
      /// /// <param name="viewer"></param>
      public DebugViewerForm(Simulator simulator, Viewer3D viewer)
      {
         InitializeComponent();

         if (simulator == null)
         {
            throw new ArgumentNullException("simulator", "Simulator object cannot be null.");
         }

         this.viewer = viewer;

         this.simulator = simulator;


         // initialise the timer used to handle user input
         UITimer = new Timer();
         UITimer.Interval = 100;
         UITimer.Tick += new EventHandler(UITimer_Tick);
         UITimer.Start();

         ViewWindow = new RectangleF(0, 0, 5000f, 5000f);
         windowSizeUpDown.Accelerations.Add(new NumericUpDownAcceleration(1, 100));

         InitImage();

      }


      /// <summary>
      /// When the user holds down the  "L", "R", "U", "D" buttons,
      /// shift the view. Avoids the case when the user has to click
      /// buttons like crazy.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void UITimer_Tick(object sender, EventArgs e)
      {
         if (DownButtonDown)
         {
            ShiftViewDown();
         }

         if (UpButtonDown)
         {
            ShiftViewUp();
         }

         if (LeftButtonDown)
         {
            ShiftViewLeft();
         }

         if (RightButtonDown)
         {
            ShiftViewRight();
         }
      }

      /// <summary>
      /// Initialises the picturebox and the image it contains. 
      /// </summary>
      private void InitImage()
      {
         pictureBox1.Width = IM_Width;
         pictureBox1.Height = IM_Height;

         if (pictureBox1.Image != null)
         {
            pictureBox1.Image.Dispose();
         }

         pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
      }

      /// <summary>
      /// Regenerates the 2D view. At the moment, examines the track network
      /// each time the view is drawn. Later, the traversal and drawing can be separated.
      /// </summary>
      private void GenerateView()
      {

         float minX = float.MaxValue;
         float minY = float.MaxValue;

         float maxX = float.MinValue;
         float maxY = float.MinValue;

         List<LineSegment> segments = new List<LineSegment>();
         List<PointF> switches = new List<PointF>();

         TrackNode[] nodes = simulator.TDB.TrackDB.TrackNodes;

         for (int i = 0; i < nodes.Length; i++)
         {
            TrackNode currNode = nodes[i];

            if (currNode != null)
            {

               if (currNode.TrVectorNode != null)
               {

                  if (currNode.TrVectorNode.TrVectorSections.Length > 1)
                  {
                     AddSegments(segments, currNode.TrVectorNode.TrVectorSections, ref minX, ref minY, ref maxX, ref maxY);
                  }
                  else
                  {
                     
                  }
               }
               else if (currNode.TrJunctionNode != null)
               {
                  switches.Add(new PointF(currNode.UiD.TileX * 2048 + currNode.UiD.X, currNode.UiD.TileZ * 2048 + currNode.UiD.Z));
               }
               else
               {
                  // for now, do nothing here... we might do something here later
               }
            }
         }
    
         using(Graphics g = Graphics.FromImage(pictureBox1.Image))
         {

            g.Clear(Color.White);

            // this is the total size of the entire viewable route (xRange == width, yRange == height in metres)
            float xRange = maxX - minX;
            float yRange = maxY - minY;

            float xScale = pictureBox1.Width / ViewWindow.Width;
            float yScale = pictureBox1.Height/ ViewWindow.Height;

            foreach (var line in segments)
            {

               PointF scaledA = new PointF((line.A.X - minX - ViewWindow.X) * xScale, (line.A.Y - minY - ViewWindow.Y) * yScale);
               PointF scaledB = new PointF((line.B.X - minX - ViewWindow.X) * xScale, (line.B.Y - minY - ViewWindow.Y) * yScale);

               g.DrawLine(Pens.Gray, scaledA, scaledB);
            }

            if (showSwitches.Checked)
            {
               foreach (PointF sw in switches)
               {
                  PointF scaledSw = new PointF((sw.X - minX - ViewWindow.X) * xScale, (sw.Y - minY - ViewWindow.Y) * yScale);

                  g.FillEllipse(Brushes.Black, Dot(scaledSw, 5f));
               }
            }

         }


         pictureBox1.Invalidate();
      }

      /// <summary>
      /// Generates a rectangle representing a dot being drawn.
      /// </summary>
      /// <param name="p">Center point of the dot, in pixels.</param>
      /// <param name="size">Size of the dot's diameter, in pixels</param>
      /// <returns></returns>
      private RectangleF Dot(PointF p, float size)
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
      private static void AddSegments(List<LineSegment> segments, TrVectorSection[] items,  ref float minX, ref float minY, ref float maxX, ref float maxY)
      {
         for (int i = 0; i < items.Length - 1; i++)
         {
            PointF A = new PointF(items[i].TileX * 2048 + items[i].X, items[i].TileZ * 2048 + items[i].Z);
            PointF B = new PointF(items[i + 1].TileX * 2048 + items[i + 1].X, items[i + 1].TileZ * 2048 + items[i+1].Z);

            CalcBounds(ref maxX, A.X, true);
            CalcBounds(ref maxY, A.Y, true);
            CalcBounds(ref maxX, B.X, true);
            CalcBounds(ref maxY, B.Y, true);

            CalcBounds(ref minX, A.X, false);
            CalcBounds(ref minY, A.Y, false);
            CalcBounds(ref minX, B.X, false);
            CalcBounds(ref minY, B.Y, false);

            segments.Add(new LineSegment(A, B));
         }
      }

      /// <summary>
      /// Given a value representing a limit, evaluate if the given value exceeds the current limit.
      /// If so, expand the limit.
      /// </summary>
      /// <param name="limit">The current limit.</param>
      /// <param name="value">The value to compare the limit to.</param>
      /// <param name="gt">True when comparison is greater-than. False if less-than.</param>
      private static void CalcBounds(ref float limit, float value, bool gt)
      {
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
         GenerateView();
      }

      private void leftButton_Click(object sender, EventArgs e)
      {
         ShiftViewLeft();
      }

      private void rightButton_Click(object sender, EventArgs e)
      {
         ShiftViewRight();
      }

      private void upButton_Click(object sender, EventArgs e)
      {
         ShiftViewUp();
      }

      private void downButton_Click(object sender, EventArgs e)
      {
         ShiftViewDown();
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




      private void windowSizeUpDown_ValueChanged(object sender, EventArgs e)
      {
         // this is the center, before increasing the size
         PointF center = new PointF(ViewWindow.X + ViewWindow.Width / 2f, ViewWindow.Y + ViewWindow.Height / 2f);


         float newSize = (float)windowSizeUpDown.Value;

         ViewWindow = new RectangleF(center.X - newSize / 2f, center.Y - newSize / 2f, newSize, newSize);


         GenerateView();

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

      private void showSwitches_CheckedChanged(object sender, EventArgs e)
      {
         GenerateView();
      }
      



   }


   /// <summary>
   /// Defines a geometric line segment.
   /// </summary>
   internal struct LineSegment
   {
      public PointF A;
      public PointF B;

      public LineSegment(PointF A, PointF B)
      {
         this.A = A;
         this.B = B;
      }
   }

}
