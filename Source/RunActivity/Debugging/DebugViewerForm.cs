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

      private readonly Viewer3D viewer;

      Timer timer = new Timer();
      

      /// <summary>
      /// Creates a new DebugViewerForm.
      /// </summary>
      /// <param name="simulator"></param>
      public DebugViewerForm(Simulator simulator, Viewer3D viewer)
      {
         InitializeComponent();



         if (simulator == null)
         {
            throw new ArgumentNullException("simulator", "Simulator object cannot be null.");
         }

         this.viewer = viewer;

         this.simulator = simulator;

         

         timer.Tick += new EventHandler(timer_Tick);
         timer.Interval = 1000;
         timer.Start();


         pictureBox1.Width = 4000;
         pictureBox1.Height = 6000;   
         pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
         
      }

      void timer_Tick(object sender, EventArgs e)
      {
         GenerateView();
      }


      private TDBTraveller GetFrontTraveller()
      {
         // for now, assume player train is FIRST train
         return new TDBTraveller(simulator.Trains[0].FrontTDBTraveller);
      }


      private float scale = 0.1f;

      private float shiftX = 500f;

      private void GenerateView()
      {
         TDBTraveller t = GetFrontTraveller();

         //t.TN

         float dotSize = 60f * scale;

         using(Graphics g = Graphics.FromImage(pictureBox1.Image))
         {

            g.Clear(Color.Transparent);

            foreach (var trackNode in simulator.TDB.TrackDB.TrackNodes)
            {
               if (trackNode != null && trackNode.UiD != null)
               {
                  var temp = GetFrontTraveller();

                  var v = temp.WorldLocation.Location.X;

                  if (/*trackNode.UiD.WorldID == 552*/ true)
                  {
                     //g.FillEllipse(Brushes.Black, Dot(trackNode.UiD.WorldTileX * 2048 + trackNode.UiD.X, trackNode.UiD.TileZ * 2048 + trackNode.UiD.Z, 2));
                     g.FillEllipse(Brushes.Black, Dot(trackNode.UiD.WorldTileX * scale + 1200, trackNode.UiD.WorldTileZ * scale, 2));

                  }



                  //PointF start = new PointF(trackNode.UiD.X, trackNode.UiD.Y);

                  //if (trackNode.TrVectorNode != null)
                  //{
                  //   foreach (var section in trackNode.TrVectorNode.TrVectorSections)
                  //   {
                  //      PointF end = new PointF(section.X, section.Y);

                  //      g.DrawLine(Pens.Black, start, end);
                  //   }
                  //}



                  //foreach (var pin in trackNode.TrPins)
                  //{
                  //   if (pin != null)
                  //   {
                  //      UiD connected = simulator.TDB.TrackDB.TrackNodes[pin.Link].UiD;
                  //      if (connected != null)
                  //      {

                  //         PointF end = new PointF(connected.X, connected.Y);

                  //         g.DrawLine(Pens.Black, start, end);
                  //      }
                  //   }
                  //}
               }



            }


            //if (viewer.SceneryDrawer != null)
            //{
            //   foreach (WorldFile wf in viewer.SceneryDrawer.WorldFiles)
            //   {
            //      foreach (var item in wf.SceneryObjects)
            //      {
            //         if (item is StaticTrackShape)
            //         {
            //            StaticTrackShape trackShape = item as StaticTrackShape;

            //            //simulator.TDB.


            //            WorldPosition wp = trackShape.Location;

            //            int tileX = wp.TileX;
            //            int tileY = wp.TileZ;

            //            if (tileX == -11064 && tileY == 14264)
            //            {

            //               double universeX = trackShape.Location.WorldLocation.Location.X + 1024;
            //               double universeY = -trackShape.Location.WorldLocation.Location.Z + 1024;

            //               //trackShape.Location.Location.


            //               g.FillEllipse(Brushes.Black, Dot((float)universeX,(float)universeY, 5));
            //            }
            //         }
            //         else if (item is StaticShape)
            //         {

            //         }
            //      }
            //   }
            //}  
         }
      }

      /// <summary>
      /// Helper function used to generate a dot for debugging purposes.
      /// </summary>
      /// <param name="X"></param>
      /// <param name="Y"></param>
      /// <param name="Size"></param>
      /// <returns></returns>
      private RectangleF Dot(float X, float Y, float Size)
      {
         return new RectangleF(X, Y, Size, Size);
      }
   }
}
