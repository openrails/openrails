/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Windows.Forms;

namespace ORTS
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay
    {
        StringBuilder TextBuilder = new StringBuilder();
        Matrix Matrix = Matrix.Identity;
        SpriteBatchMaterial Material;
        TextPrimitive TextPrimitive = new TextPrimitive();
        Viewer3D Viewer;
        int InfoAmount = 1;
        private double lastUpdateTime = 0;   // update text message only 10 times per second

        int processors = System.Environment.ProcessorCount;

        public InfoDisplay( Viewer3D viewer )
        {
            Viewer = viewer;
            // Create a new SpriteBatch, which can be used to draw text.
            Material = (SpriteBatchMaterial) Materials.Load( Viewer.RenderProcess, "SpriteBatch" );
            TextPrimitive.Material = Material;
            TextPrimitive.Color = Color.Yellow;
            TextPrimitive.Location = new Vector2(10, 10);
        }

        public void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(Microsoft.Xna.Framework.Input.Keys.F5))
            {
                ++InfoAmount;
                if (InfoAmount > 2)
                    InfoAmount = 0;
            }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (Program.RealTime - lastUpdateTime > 0.3)
            {
                double elapsedRealSeconds = Program.RealTime - lastUpdateTime;
                lastUpdateTime = Program.RealTime;
                Profile( elapsedRealSeconds);
                UpdateText();
            }
            TextPrimitive.Text = TextBuilder.ToString();
            frame.AddPrimitive( Material, TextPrimitive, ref Matrix);
        }


        public void UpdateText()
        {
            TextBuilder.Length = 0;

            if (InfoAmount > 0)
            {
                AddBasicInfo();
            }
            if (InfoAmount > 1)
            {
                AddDebugInfo();
            }
        }

        private void AddBasicInfo()
        {
            string clockTimeString = FormattedTime(Viewer.Simulator.ClockTime);

            TextBuilder.Append("Version = "); TextBuilder.AppendLine(Program.Revision);
            TextBuilder.Append("Time = "); TextBuilder.AppendLine(clockTimeString);
            TextBuilder.Append("Direction = ");
            if (Math.Abs(Viewer.PlayerLocomotive.Train.MUReverserPercent) != 100)
                TextBuilder.Append(string.Format("{0}% ", Math.Abs(Viewer.PlayerLocomotive.Train.MUReverserPercent)));
            TextBuilder.AppendLine(Viewer.PlayerLocomotive.Direction.ToString());
            TextBuilder.Append("Throttle = "); TextBuilder.AppendLine(Viewer.PlayerLocomotive.ThrottlePercent.ToString());
            TextBuilder.Append("Brake = "); TextBuilder.AppendLine(Viewer.PlayerLocomotive.BrakeSystem.GetStatus());
            TextBuilder.Append("Speed = "); TextBuilder.AppendLine(MpH.FromMpS(Math.Abs(Viewer.PlayerLocomotive.SpeedMpS)).ToString("F1"));
            string status = Viewer.PlayerLocomotive.GetStatus();
            if (status != null)
                TextBuilder.AppendLine(status);

            // Added by rvg....
            // Compass
            string sTemp;
            Vector2 compassDir;
            compassDir.X = Viewer.Camera.XNAView.M11;
            compassDir.Y = Viewer.Camera.XNAView.M13;
            float direction = MathHelper.ToDegrees((float)Math.Acos(compassDir.X));
            if (compassDir.Y > 0) direction = 360-direction;
            sTemp = direction.ToString("N0");
            sTemp += Convert.ToChar(176);
            TextBuilder.Append("Compass Hdg: "); TextBuilder.AppendLine(sTemp);
            // Latitude/Longitude
            WorldLatLon worldLatLon = new WorldLatLon();
            double latitude = 0;
            double longitude = 0; ;
            worldLatLon.ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref latitude, ref longitude);
            sTemp = MathHelper.ToDegrees((float)latitude).ToString("F6");
            sTemp += ", ";
            sTemp += MathHelper.ToDegrees((float)longitude).ToString("F6");
            TextBuilder.Append("Lat/Lon: "); TextBuilder.AppendLine(sTemp);

            status = Viewer.Simulator.AI.GetStatus();
            if (status != null)
                TextBuilder.AppendLine(status);

            TextBuilder.AppendLine();
            TextBuilder.Append("FPS = "); TextBuilder.AppendLine(Math.Round(Viewer.RenderProcess.SmoothedFrameRate).ToString());
        }

        [Conditional("DEBUG")]
        private void AddDebugInfo()
        {
            // Memory Useage
            long memory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            TextBuilder.AppendLine();
            TextBuilder.Append("Build = "); TextBuilder.AppendLine(Program.Build);
            TextBuilder.Append("Memory = "); TextBuilder.AppendLine(memory.ToString());
            TextBuilder.Append("Jitter = "); TextBuilder.AppendLine(Viewer.RenderProcess.SmoothJitter.ToString("F4"));
            TextBuilder.Append("Primitives = "); TextBuilder.AppendLine(Viewer.RenderProcess.PrimitivesPerFrame.ToString());
            TextBuilder.Append("StateChanges = "); TextBuilder.AppendLine(Viewer.RenderProcess.RenderStateChangesPerFrame.ToString());
            TextBuilder.Append("ImageChanges = "); TextBuilder.AppendLine(Viewer.RenderProcess.ImageChangesPerFrame.ToString());
            TextBuilder.Append("Processors = "); TextBuilder.AppendLine(processors.ToString());
            TextBuilder.Append("Render Process % = "); TextBuilder.AppendLine( string.Format( "{0,3}",RenderPercent ));
            TextBuilder.Append("Update Process % = "); TextBuilder.AppendLine( string.Format( "{0,3}", UpdatePercent));
            TextBuilder.Append("Loader Process % = "); TextBuilder.AppendLine( string.Format( "{0,3}",LoaderPercent));
            TextBuilder.Append("Total Process % = "); TextBuilder.AppendLine(string.Format("{0,3}", LoaderPercent+UpdatePercent+RenderPercent));
            // Added by rvg....
            TextBuilder.Append("Tile: "); TextBuilder.Append(Viewer.Camera.TileX.ToString()); // Camera coordinates
            TextBuilder.Append(" ");
            TextBuilder.Append(Viewer.Camera.TileZ.ToString());
            TextBuilder.Append(" ");
            TextBuilder.AppendLine(Viewer.Camera.Location.ToString());
        }

        string FormattedTime(double clockTimeSeconds)
        {
            int hour = (int)(clockTimeSeconds / (60.0 * 60.0));
            clockTimeSeconds -= hour * 60.0 * 60.0;
            int minute = (int)(clockTimeSeconds / 60.0);
            clockTimeSeconds -= minute * 60.0;
            int seconds = (int)clockTimeSeconds;
            // Reset clock before and after midnight
            if (hour >= 24)
                hour -= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, seconds);
        }

        // Profiling
        int RenderPercent = 0;
        int UpdatePercent = 0;
        int LoaderPercent = 0;
        double lastRender = 0;        // render work  ( seconds )
        double lastRenderUpdate = 0;  // the update work done by the render process
        double lastUpdater = 0;        // update work done by the update process
        double lastLoader = 0;        // loader work
        public void Profile(double elapsedRealSeconds) // should be called every 100mS
        {
            if( elapsedRealSeconds < 0.01 ) return;  // just in case

            // capture time
            double render = Viewer.RenderProcess.RenderTime.Elapsed.TotalSeconds;
            double renderupdate = Viewer.RenderProcess.UpdateTime.Elapsed.TotalSeconds;
            double update = Viewer.UpdaterProcess == null ? 0 :  Viewer.UpdaterProcess.UpdateTimer.Elapsed.TotalSeconds;
            double loader = Viewer.LoaderProcess.LoaderTimer.Elapsed.TotalSeconds;

            // determine elapsed times
            //    note - these processing times are approximate and assume the task had the processor for the full time
            //           in reality the processor could have been interupted to service other tasks
            double elapsedRender = render - lastRender;
            double elapsedRenderUpdate = renderupdate - lastRenderUpdate;
            double elapsedLoader = loader - lastLoader;
            double elapsedUpdater = update - lastUpdater; 

            // save last times
            lastRender = render;
            lastRenderUpdate = renderupdate;
            lastUpdater = update;
            lastLoader = loader;

            // computer percentages
            elapsedRender -= elapsedRenderUpdate;
            elapsedUpdater += elapsedRenderUpdate;

            RenderPercent = (int)(elapsedRender * 100.0 / elapsedRealSeconds);
            UpdatePercent = (int)(elapsedUpdater * 100.0 / elapsedRealSeconds);
            LoaderPercent = (int)(elapsedLoader * 100.0 / elapsedRealSeconds);
           
        }

    } // Class Info Display

    public class TextPrimitive : RenderPrimitive
    {
        public SpriteBatchMaterial Material;
        public string Text;
        public Color Color;
        public Vector2 Location;

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            Material.SpriteBatch.DrawString(Material.DefaultFont, Text, Location, Color );
        }
    }



}