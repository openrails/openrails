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


namespace ORTS
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay: RenderPrimitive
    {
        string  Text = "";
        Matrix Matrix = Matrix.Identity;
        SpriteBatchMaterial Material;
        Viewer3D Viewer;

        public double SmoothedFrameRate = 1000;     // information displayed by InfoViewer in upper left
        public double MinFrameRate = 1000;

        private double lastClockUpdate = 0;
        private string ClockTimeString = "";

        int processors = System.Environment.ProcessorCount;

        double SmoothJitter = 0;

        public InfoDisplay( Viewer3D viewer )
        {
            Viewer = viewer;
            // Create a new SpriteBatch, which can be used to draw text.
            Material = (SpriteBatchMaterial) Materials.Load( Viewer.RenderProcess, "SpriteBatch" );
        }

        public void Update(GameTime gameTime)
        {
            if (Viewer.Simulator.ClockTimeSeconds - lastClockUpdate > 1)  // Update very second
            {
                ClockTimeString = FormattedTime(Viewer.Simulator.ClockTimeSeconds);
                lastClockUpdate = Viewer.Simulator.ClockTimeSeconds;
            }

            // Memory Useage
            long memory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            Text = "Version = " + Program.Version + "\n";
            Text = Text + "Time = " + ClockTimeString + "\n";
            Text = Text + "Direction = " + (Viewer.Simulator.PlayerLocomotive.Forward ? "FORWARD\n" : "REVERSE\n");
            Text = Text + "Throttle = " + Viewer.Simulator.PlayerLocomotive.ThrottlePercent.ToString() + "\n";
            Text = Text + "Brake = " + Viewer.Simulator.PlayerTrain.TrainBrakePercent.ToString() + "\n";
            Text = Text + "Speed = " + MpH.FromMpS(Math.Abs(Viewer.Simulator.PlayerLocomotive.SpeedMpS)).ToString("F1") + "\n";
            Text = Text + "\n";
            Text = Text + "Memory = " + memory.ToString() + "\n";
            Text = Text + "FPS = " + Math.Round(SmoothedFrameRate).ToString() + "\n";
            Text = Text + "Jitter = " + SmoothJitter.ToString("F4") + "\n";
            Text = Text + "Primitives = " + Viewer.RenderProcess.PrimitivesPerFrame.ToString() + "\n";
            Text = Text + "StateChanges = " + Viewer.RenderProcess.RenderStateChangesPerFrame.ToString() + "\n";
            Text = Text + "ImageChanges = " + Viewer.RenderProcess.ImageChangesPerFrame.ToString() + "\n";
            Text = Text + "Processors = " + processors.ToString() + "\n";
            if (Viewer.RenderProcess.UpdateSlow)
                Text = Text + "\r\nUpdate Slow";
            if (Viewer.RenderProcess.LoaderSlow)
                Text = Text + "\r\nLoader Slow";
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void PrepareFrame( RenderFrame frame, GameTime gameTime)
        {
            // Smoothing filter length
            double seconds = gameTime.ElapsedRealTime.TotalSeconds;
            double rate = 5.0/seconds;
            

            // Jitter
            if( seconds > 0 )
            {
                double jitter = Math.Abs(Viewer.RenderProcess.Jitter);
                if (Math.Abs(jitter - SmoothJitter) > 0.01 )
                    SmoothJitter = jitter;
                else
                    SmoothJitter = SmoothJitter * (rate - 1.0) / rate + jitter * 1.0 / rate;
            }

            // Frame Rate
            int elapsedMS = gameTime.ElapsedGameTime.Milliseconds;

            if (elapsedMS != 0)
            {
                double frameRate = 1000f / elapsedMS;

                if (frameRate < MinFrameRate)
                    MinFrameRate = frameRate;
                else
                    MinFrameRate = ((MinFrameRate * 19f) + frameRate) / 20f;

                if (Math.Abs(frameRate - SmoothedFrameRate) > 5)
                    SmoothedFrameRate = frameRate;
                else
                    SmoothedFrameRate = SmoothedFrameRate * (rate - 1.0) / rate + frameRate * 1.0 / rate;
            }


            frame.AddPrimitive( Material, this, ref Matrix);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Draw(GraphicsDevice graphicsDevice)
        {
            Material.SpriteBatch.DrawString(Material.DefaultFont, Text, new Vector2(5, 10), Color.Yellow);
        }


        string FormattedTime(double clockTimeSeconds)
        {
            int hour = (int)(clockTimeSeconds / (60.0 * 60.0));
            clockTimeSeconds -= hour * 60.0 * 60.0;
            int minute = (int)(clockTimeSeconds / 60.0);
            clockTimeSeconds -= minute * 60.0;
            int seconds = (int)clockTimeSeconds;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, seconds);
        }


    }
}