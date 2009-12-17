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
using MSTS;
using System.Diagnostics;
using System.Threading;


namespace ORTS
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay : Microsoft.Xna.Framework.DrawableGameComponent
    {
        SpriteBatch SpriteBatch;
        SpriteFont CourierNew;
        private Viewer Viewer;

        public double SmoothedFrameRate = 1000;     // information displayed by InfoViewer in upper left
        public double MinFrameRate = 1000;

        public InfoDisplay( Viewer viewer)
            : base(viewer)
        {
            Viewer = viewer;

            this.DrawOrder = 100;
            Viewer.Components.Add(this);


        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw text.
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            CourierNew =  Viewer.Content.Load<SpriteFont>("CourierNew");
            base.LoadContent();
        }
        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            
            Viewer.Text = "Direction = " + (Viewer.Simulator.PlayerLocomotive.Forward ? "FORWARD\n" : "REVERSE\n");
            Viewer.Text = Viewer.Text + "Throttle = " + Viewer.Simulator.PlayerLocomotive.ThrottlePercent.ToString() + "\n";
            Viewer.Text = Viewer.Text + "Brake = " + Viewer.Simulator.PlayerTrain.TrainBrakePercent.ToString() + "\n";
            Viewer.Text = Viewer.Text + "Speed = " + MpH.FromMpS(Math.Abs(Viewer.Simulator.PlayerLocomotive.SpeedMpS)).ToString() + "\n";
            

            //TODO, REMOVE TDB DEBUG STUFF 
            //Viewer.Text += "\n\n" + tdb.ToString() + string.Format( " D={0}\n",tdb.Direction ) +
            //    String.Format("X={0} Y={1} Z={2} AX={3} AY={4} AZ={5}", tdb.X,tdb.Y,tdb.Z, tdb.AX, tdb.AY, tdb.AZ);
            
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {

            Color color = Color.Yellow;

            // Memory Useage
            long memory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;


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
                    SmoothedFrameRate = ((SmoothedFrameRate * 19f) + frameRate) / 20f;
            }

            // Note- don't use a negative DepthBias anywhere - or it results in this text being drawn underneath the scenery.
            // Draw the string
            SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            SpriteBatch.DrawString(CourierNew, "Version = " + Program.Version, new Vector2(20, 20), color);
            SpriteBatch.DrawString(CourierNew, "Memory = " + memory.ToString(), new Vector2(20, 40), color); 
            SpriteBatch.DrawString(CourierNew, "FPS = " + Math.Round(SmoothedFrameRate).ToString(), new Vector2(20, 60), color);
            SpriteBatch.DrawString(CourierNew, Viewer.Text, new Vector2(20, 80), color);
            SpriteBatch.End();

            base.Draw(gameTime);
        }


    }
}