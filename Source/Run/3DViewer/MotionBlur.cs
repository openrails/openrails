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
    public class MotionBlur : Microsoft.Xna.Framework.DrawableGameComponent
    {
        SpriteBatch spritebatch;
        private Viewer Viewer;

        ResolveTexture2D colorBuffer;

        public new bool Enabled = true;


        public MotionBlur( Viewer viewer)
            : base(viewer)
        {
            Viewer = viewer;

            this.DrawOrder = 50;
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
            spritebatch = new SpriteBatch(GraphicsDevice);
            ScreenChanged();
            base.LoadContent();
        }

        public void ScreenChanged()
        {
            PresentationParameters pp = GraphicsDevice.PresentationParameters;
            colorBuffer = new ResolveTexture2D(GraphicsDevice,
                                               pp.BackBufferWidth,
                                               pp.BackBufferHeight,
                                               1,
                                               pp.BackBufferFormat);

            //The following lines of code clear 
            //the ResolveTexture2D to (0,0,0,0)
            GraphicsDevice.Clear(Color.TransparentBlack);
            GraphicsDevice.ResolveBackBuffer(colorBuffer);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            if (Enabled)
            {
                float elapsedmilliseconds = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                Byte blurFactor = (byte)(16f * 150f / elapsedmilliseconds);  // ie 150 at 60 fps

                Viewer.RenderState = 4;

                spritebatch.Begin(SpriteBlendMode.AlphaBlend,
                                  SpriteSortMode.Immediate,
                                  SaveStateMode.SaveState);

                spritebatch.Draw(colorBuffer, new Vector2(0, 0), new Color(255, 255, 255, blurFactor));
                spritebatch.End();

                GraphicsDevice.ResolveBackBuffer(colorBuffer);


                //Now we redraw the screne after the ResolveBackBuffer call
                spritebatch.Begin(SpriteBlendMode.None,
                                  SpriteSortMode.Immediate,
                                  SaveStateMode.SaveState);

                spritebatch.Draw(colorBuffer, new Vector2(0, 0), Color.White);
                spritebatch.End();
            }

            base.Draw(gameTime);
        }


    }
}