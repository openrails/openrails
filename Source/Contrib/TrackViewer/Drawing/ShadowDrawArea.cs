// COPYRIGHT 2014 by the Open Rails project.
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

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// Delegate so the calling class can give a routine that is used for actual drawing
    /// </summary>
    /// <param name="drawArea">The draw area to draw upon</param>
    public delegate void DrawingDelegate(DrawArea drawArea);

    ///<summary>
    /// This class extends DrawArea. DrawArea itself is a class that handles all translation of real world coordinates 
    /// to pixels. This class creates the possibility to create a cache of (part of) what is drawn on the drawArea
    /// Redrawing some things completely, even when not necessary, creates a large overhead. Here re-drawing is prevented
    /// by drawing first to a texture, only when needed, saving the texture, and drawing the texture on screen each pass.
    /// In this way the expensive drawing routine is only called when needed.
    /// 
    /// In this class we have two 'shadow' textures. The drawing area itself will be drawn upon in a number of subblocks
    /// Ninner * Ninner. Around that a number of blocks will also be drawn (but not immediately visible) to a total of
    /// Nouter * Nouter. Together these blocks will be drawn on a combined texture.
    /// Once the combined texture is ready, it is easy to shift the location: Simply a different part of the combined texture
    /// will be shown, and no (costly) redrawing is needed, until we get out of bounds.
    /// Also zooming is possibly, by zooming in or out of the combined texture. Redrawing will be done on zooming in and out
    /// soon afterwards, because zooming effects are visible.
    /// 
    /// Since for the expensive drawing routine the actual number of pixels it draws to is not very relevant (the expense
    /// is more in the amount of things that need to be drawn, not in the rendering to pixels itself), we also
    /// oversample the drawing a bit: we draw at (for instance) twice the resolution. This gives much better zoom behaviour.
    /// 
    /// 
    /// More visible blocks reduces the time needed to draw a sub-block, improving performance but costing more time to have complete
    /// visible region redrawn (since total draw time is not changed)
    /// More non-visible blocks reduces a bit performance (but in many cases only in background), but reduces
    /// the number of needed redraw when shifting.
    ///
    /// The most important methods are
    ///     Constructor (which does nothing specifically to shadowing)
    ///     SetScreenSize. Set the size of the screen in pixels to draw upon. Also called after window resize
    ///     LoadContent. This is where the amount of blocks to be used are defined
    ///     
    ///     Normal use of a drawArea would be
    ///         somewhere in Game.Draw method:      DrawSomething(drawArea);
    ///     For use shadowing use this instead:
    ///         somewhere in Game.Draw method:                          yourShadowDrawArea.DrawShadowedTexture();
    ///         Also in the Game.Draw, but before spriteBatch.begin:    yourShadowDrawArea.DrawShadowTextures(DrawSomething, background color);
    ///
    ///</summary>
    public class ShadowDrawArea : DrawArea, IDisposable
    {
        #region private Fields
        int blockW; // Width  of single block in pixels
        int blockH; // Height of single block in pixels

        // basic scaling equations: pixelX = scale * (worldX - offsetX)
        double shadowScale;     // scale of the current combined shadow texture in pixels/meter
        double shadowOffsetX;   // lower-left location (in real world coordinates) of the combined texture
        double shadowOffsetZ;   // lower-left location (in real world coordinates) of the combined texture

        // fields related to when and what we are re-drawing
        bool needToRedraw = true;
        bool needToRedrawLater = true;
        bool[] needToDrawRectangle; // Array of booleans describing which of the rectangles still need to be redrawn
        int nextRectangleToDraw; // integer from 0 to Nblocks-1, describing which of the subblocks will be drawn

        GraphicsDevice graphicsDevice;  // we need a graphics device so we can draw to texture
        SpriteBatch spriteBatch;        // also for drawing to texture

        // textures and rendertarget for blocks
        RenderTarget2D[] shadowRenderTargetSingle;
        Texture2D[] shadowMapsSingle;
        ShadowDrawArea shadowDrawArea; // draw area used to draw individual blocks. 
                                       // A normal drawArea would suffice, but this would not give access to protected members!

        // texture and rendertarget for combined texture
        RenderTarget2D shadowRenderTargetCombined;
        Texture2D shadowMapCombined;

        // fields related to subblocks
        int Nsubblocks; // total amount of subblocks
        int Ninner;    // Number of (1D) blocks in visible region
        int Nouter;    // Number of (1D) blocks total.
        int Nborder { get { return (Nouter - Ninner) / 2; } } // number of blocks in border
        int Nsampling; // Oversampling rate.
        int[] xOrder; // array containing the x-indexes of the subblocks in the order they need to be drawn
        int[] zOrder; // array containing the z-indexes of the subblocks in the order they need to be drawn
        int[][] orderFromLocation; // array that contains the order-index, given the x, and z-indexes of the subblock.

        #endregion

        /// <summary>
        /// Constructor. Just calling the base constructor
        /// </summary>
        public ShadowDrawArea(DrawScaleRuler drawScaleRuler)
            : base(drawScaleRuler)
        {
            shadowDrawArea = new ShadowDrawArea();
        }

        /// <summary>
        /// Constructor of a shadowDrawArea that is a child of another shadowDrawArea.
        /// We only want it to be a drawArea, but do need access to protected members
        /// Therefore, it is a shadowDrawArea, but we will not use any of its additional functionality.
        /// Hence also private
        /// </summary>
        ShadowDrawArea() : base(null) { }

        /// <summary>
        /// Sets the screen size on which we can draw (in pixels). 
        /// </summary>
        /// <param name="areaOffsetX">x-position of the top-left corner of the draw-area, in pixels</param>
        /// <param name="areaOffsetY">y-position of the top-left corner of the draw-area, in pixels</param>
        /// <param name="areaWidth">width of the area to draw upon, in pixels</param>
        /// <param name="areaHeight">height of the area to draw upon, in pixels</param>
        public override void SetScreenSize(int areaOffsetX, int areaOffsetY, int areaWidth, int areaHeight)
        {
            base.SetScreenSize(areaOffsetX, areaOffsetY, areaWidth, areaHeight);
            SetRendertargetSizes();
            needToRedraw = true;
        }

        /// <summary>
        /// Store the graphics device and spiteBatch, set the amount of blocks we use
        /// </summary>
        /// <param name="graphicsDevice">The device used for our renderTarget</param>
        /// <param name="spriteBatch">The spritebatch to use for our renderTarget</param>
        /// <param name="visibleBlocksWidth">Amount of subblocks (in 1 direction) in the visible region</param>
        /// <param name="borderBlocksWidth">Amount of subblocks (in 1 direction) in the border (same on top, left, right and bottom)</param>
        /// <param name="overSampling">Oversampling used such that zooming is looks better (2 is a good value)</param>
        public void LoadContent(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch,
            short visibleBlocksWidth, short borderBlocksWidth, short overSampling)
        {
            this.graphicsDevice = graphicsDevice;
            this.spriteBatch = spriteBatch;  // We need to use the same spriteBatch, because BasicShapes depends on it.
            Ninner = visibleBlocksWidth;
            Nouter = visibleBlocksWidth + 2 * borderBlocksWidth;
            Nsampling = overSampling;

            Nsubblocks = Nouter * Nouter; ;
            FillXYorder();

            shadowRenderTargetSingle = new RenderTarget2D[Nsubblocks];
            shadowMapsSingle = new Texture2D[Nsubblocks];

            SetRendertargetSizes();
        }

        /// <summary>
        /// If needed dispose of previously used rendertargets and textures. Create new rendertargets and textures
        /// based on new or updated screen size.
        /// </summary>
        private void SetRendertargetSizes()
        {
            if (graphicsDevice == null) { return; }

            blockW = (AreaW * Nsampling + Ninner - 1) / Ninner; // in case areaW*Nsampling is not a multiple of Ninner this
            blockH = (AreaH * Nsampling + Ninner - 1) / Ninner; // makes sure block size at least covers all of area, to prevent constant redrawing

            if (shadowRenderTargetCombined != null)
            {
                shadowRenderTargetCombined.Dispose();
            }
            if (shadowMapCombined != null)
            {
                shadowMapCombined.Dispose();
            }
            shadowRenderTargetCombined = new RenderTarget2D(graphicsDevice, Nouter * blockW, Nouter * blockH, false, SurfaceFormat.Color,
                DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            shadowDrawArea.SetScreenSize(0, 0, blockW, blockH);

            // create initial empty subtextures
            for (int i = 0; i < Nsubblocks; i++)
            {
                if (shadowRenderTargetSingle[i] != null)
                {
                    shadowRenderTargetSingle[i].Dispose();
                }
                if (shadowMapsSingle[i] != null)
                {
                    shadowMapsSingle[i].Dispose();
                }
                shadowRenderTargetSingle[i] = new RenderTarget2D(graphicsDevice, 1 * blockW, 1 * blockH, false, SurfaceFormat.Color,
                    DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
                shadowMapsSingle[i] = new Texture2D(graphicsDevice, 1, 1);
            }
        }

        /// Filling the order in which the sub blocks will be generated is a bit tricky. Hence in a separate method.
        /// The order is important because we first want to make sure we draw what will be visible (or visible soon)
        /// We start in the middle, and move outward, in rings 
        void FillXYorder()
        {
            needToDrawRectangle = new bool[Nsubblocks]; // all false by default
            xOrder = new int[Nsubblocks];
            zOrder = new int[Nsubblocks];

            int orderIndex = 0;
            int Nrings = Nouter / 2; // rounded down for odd Nouter

            // if odd number of sides, we have a single middle one.
            if (Ninner % 2 == 1)
            {   // start with the middle one.
                xOrder[orderIndex] = Nrings;
                zOrder[orderIndex] = Nrings;
                orderIndex++;
            }

            for (int iRing = Nrings - 1; iRing >= 0; iRing--)   // work from the inside out.
            {
                int Nside = Nouter - 1 - 2 * iRing;  // The amount of blocks - 1 in a side, for the given ring. Alwasy Nouter - 1 for last ring!
                //left side
                for (int j = 1; j <= Nside; j++)
                {
                    xOrder[orderIndex] = iRing;
                    zOrder[orderIndex] = iRing + j;
                    orderIndex++;
                }
                //top side
                for (int j = 1; j <= Nside; j++)
                {
                    xOrder[orderIndex] = iRing + j;
                    zOrder[orderIndex] = Nouter - 1 - iRing;
                    orderIndex++;
                }
                //right side
                for (int j = 1; j <= Nside; j++)
                {
                    xOrder[orderIndex] = Nouter - 1 - iRing;
                    zOrder[orderIndex] = Nouter - 1 - iRing - j;
                    orderIndex++;
                }
                //bottom side
                for (int j = 1; j <= Nside; j++)
                {
                    xOrder[orderIndex] = Nouter - 1 - iRing - j;
                    zOrder[orderIndex] = iRing;
                    orderIndex++;
                }
            }

            orderFromLocation = new int[Nouter][];
            for (int col = 0; col < Nouter; col++)
            {
                orderFromLocation[col] = new int[Nouter];
            }
            for (orderIndex = 0; orderIndex < Nsubblocks; orderIndex++)
            {
                orderFromLocation[xOrder[orderIndex]][zOrder[orderIndex]] = orderIndex;
            }
        }

        /// <summary>
        /// Call the supplied routine to draw on one of the subblocks.
        /// The assumption is that this routine will be called regularly (e.g. during Draw or Update) to make sure
        /// the various subblocks will indeed be drawn.
        /// This method will determine itself when there is a need for redrawing
        /// </summary>
        /// <param name="drawRoutine">The routine that is used to draw to the drawing area</param>
        /// <param name="backgroundColor">Background color to use. Using transparent is possible but will not look well with oversampling.</param>
        public void DrawShadowTextures(DrawingDelegate drawRoutine, Color backgroundColor)
        {
            DetermineRedrawingNeeds(); // did we shift out of bounds, did we zoom, ...

            if (needToRedraw)
            {
                SetNativeScales();
                AddAllToRedrawList();
                needToRedraw = false;
            }

            // if we have redrawn the inner part, we might need to redraw it (before we draw the non-visible rings)
            if ((nextRectangleToDraw >= Ninner * Ninner) && needToRedrawLater)
            {
                SetNativeScales();
                AddAllToRedrawList();
                needToRedrawLater = false;
                needToRedraw = false;
            }

            // if we have redrawn all, we are ready
            if (nextRectangleToDraw >= Nsubblocks)
            {
                return;
            }

            // Actual rendering of a single block and then all blocks to the combined texture
            RenderASingleBlockTexture(backgroundColor, drawRoutine);
            RenderCombinedTexture(backgroundColor);

            SetNextRectangleToDraw(false); // in the next (game) loop, we will draw the next one.   
        }

        /// <summary>
        /// Determine whether there is a need to redraw immediately, or possibly a bit later. 
        /// Redrawing is needed when zooming is done too much or when shifted out of bounds
        /// </summary>
        private void DetermineRedrawingNeeds()
        {
            //Real widths of inset and combined shadow texture
            double insetRealW = AreaW / Scale;
            double insetRealH = AreaH / Scale;
            double shadowRealW = Nouter * blockW / shadowScale;
            double shadowRealH = Nouter * blockH / shadowScale;

            // we really need to redraw if we are out of bounds
            if (OffsetX < shadowOffsetX) { needToRedraw = true; }
            if (OffsetZ < shadowOffsetZ) { needToRedraw = true; }
            if (OffsetX + insetRealW > shadowOffsetX + shadowRealW) { needToRedraw = true; }
            if (OffsetZ + insetRealH > shadowOffsetZ + shadowRealH) { needToRedraw = true; }

            // we will also redraw if zoom is getting too small. 
            // Visible performance is very much affected by this!
            if (Nsampling * Scale > 2.0 * shadowScale) { needToRedraw = true; }
            // when Nsampling * scale becomes too small, we will redraw because of out-of-bounds anyway

            if (needToRedraw) { return; }

            if (Nsampling * Scale > 1.1 * shadowScale) { needToRedrawLater = true; }
            if (Nsampling * Scale < 0.9 * shadowScale) { needToRedrawLater = true; }

            double ShiftLimitInBlocks = (Nborder > 1) ? Nborder - 1 : 0.5;
            double shiftLimitX = ShiftLimitInBlocks * blockW / shadowScale;
            double shiftLimitZ = ShiftLimitInBlocks * blockH / shadowScale;
            if (OffsetX < shadowOffsetX + shiftLimitX) { ShiftBlocks(ShiftDirection.Right); }
            if (OffsetZ < shadowOffsetZ + shiftLimitZ) { ShiftBlocks(ShiftDirection.Up); }
            if (OffsetX + insetRealW > shadowOffsetX + shadowRealW - shiftLimitX) { ShiftBlocks(ShiftDirection.Left); }
            if (OffsetZ + insetRealH > shadowOffsetZ + shadowRealH - shiftLimitZ) { ShiftBlocks(ShiftDirection.Down); }

            //double ringW = Nborder * blockW / shadowScale;
            //double ringH = Nborder * blockH / shadowScale;
            ////we will redraw later in case we are close, we do not want to do this too much though
            //if (offsetX < shadowOffsetX + 0.2 * ringW) { needToRedrawLater = true; }
            //if (offsetZ < shadowOffsetZ + 0.2 * ringH) { needToRedrawLater = true; }
            //if (offsetX + insetRealW > shadowOffsetX + shadowRealW - 0.2 * ringW) { needToRedrawLater = true; }
            //if (offsetZ + insetRealH > shadowOffsetZ + shadowRealH - 0.2 * ringW) { needToRedrawLater = true; }

        }

        /// <summary>
        /// Set flags for all subblocks to be redrawn, and make sure the first one will be redrawn first
        /// </summary>
        private void AddAllToRedrawList()
        {
            for (int orderIndex = 0; orderIndex < Nsubblocks; orderIndex++)
            {
                needToDrawRectangle[orderIndex] = true;
            }
            nextRectangleToDraw = 0;
        }

        /// <summary>
        /// Determine what the next rectangle will be that will be redrawn in the next loop
        /// </summary>
        /// <param name="recheck">do we need to recheck from beginning (in case some items have been added</param>
        private void SetNextRectangleToDraw(bool recheck)
        {
            if (recheck) { nextRectangleToDraw = -1; }
            do
            {
                nextRectangleToDraw++;
            } while (nextRectangleToDraw < Nsubblocks && !needToDrawRectangle[nextRectangleToDraw]);
        }

        enum ShiftDirection { Left, Right, Up, Down };

        /// <summary>
        /// Instead of redrawing a lot of subblocks, we will just move most of the blocks by one block to any direction
        /// and only mark a single column or row for redrawing.
        /// </summary>
        /// <param name="direction">The direction to shift</param>
        private void ShiftBlocks(ShiftDirection direction)
        {

            switch (direction)
            {
                case ShiftDirection.Right:
                    ShiftSubBlocksRight();
                    shadowOffsetX -= blockW / shadowScale;
                    break;
                case ShiftDirection.Left:
                    ShiftSubBlocksLeft();
                    shadowOffsetX += blockW / shadowScale;
                    break;
                case ShiftDirection.Up:
                    ShiftSubBlocksUp();
                    shadowOffsetZ -= blockH / shadowScale;
                    break;
                case ShiftDirection.Down:
                    ShiftSubBlocksDown();
                    shadowOffsetZ += blockH / shadowScale;
                    break;
            }

            // make sure the textures link to the correct render targets
            for (int orderIndex = 0; orderIndex < Nsubblocks; orderIndex++)
            {
                try
                {   // I got errors: The render target must not be set on the device when calling GetTexture()
                    // even if I just before this set graphicsDevice.SetRenderTarget(0, null); I have no idea why
                    shadowMapsSingle[orderIndex] = shadowRenderTargetSingle[orderIndex];
                }
                catch { }
            }

            SetNextRectangleToDraw(true);
        }

        /// <summary>
        /// The real world location is so far left, that we can discard the right-most column of blocks and add a new column on the left
        /// </summary>
        private void ShiftSubBlocksRight()
        {
            for (int row = 0; row < Nouter; row++)
            {
                RenderTarget2D tempTarget = shadowRenderTargetSingle[orderFromLocation[Nouter - 1][row]];
                for (int col = Nouter - 2; col >= 0; col--)
                {
                    shadowRenderTargetSingle[orderFromLocation[col + 1][row]]
                        = shadowRenderTargetSingle[orderFromLocation[col][row]];
                }
                shadowRenderTargetSingle[orderFromLocation[0][row]] = tempTarget;
                needToDrawRectangle[orderFromLocation[0][row]] = true;
            }
        }

        /// <summary>
        /// The real world location is so far right, that we can discard the right-most column of blocks and add a new column on the right
        /// </summary>
        private void ShiftSubBlocksLeft()
        {
            for (int row = 0; row < Nouter; row++)
            {
                RenderTarget2D tempTarget = shadowRenderTargetSingle[orderFromLocation[0][row]];
                for (int col = 1; col < Nouter; col++)
                {
                    shadowRenderTargetSingle[orderFromLocation[col - 1][row]]
                        = shadowRenderTargetSingle[orderFromLocation[col][row]];
                }
                shadowRenderTargetSingle[orderFromLocation[Nouter - 1][row]] = tempTarget;
                needToDrawRectangle[orderFromLocation[Nouter - 1][row]] = true;
            }
        }

        /// <summary>
        /// The real world location is so far down, that we can discard the top-most column of blocks and add a new column on the bottom
        /// </summary>
        private void ShiftSubBlocksUp()
        {
            for (int col = 0; col < Nouter; col++)
            {
                RenderTarget2D tempTarget = shadowRenderTargetSingle[orderFromLocation[col][Nouter - 1]];
                for (int row = Nouter - 2; row >= 0; row--)
                {
                    shadowRenderTargetSingle[orderFromLocation[col][row + 1]]
                        = shadowRenderTargetSingle[orderFromLocation[col][row]];
                }
                shadowRenderTargetSingle[orderFromLocation[col][0]] = tempTarget;
                needToDrawRectangle[orderFromLocation[col][0]] = true;
            }
        }

        /// <summary>
        /// The real world location is so far up, that we can discard the bottom-most column of blocks and add a new column on the top
        /// </summary>
        private void ShiftSubBlocksDown()
        {
            for (int col = 0; col < Nouter; col++)
            {
                RenderTarget2D tempTarget = shadowRenderTargetSingle[orderFromLocation[col][0]];
                for (int row = 1; row < Nouter; row++)
                {
                    shadowRenderTargetSingle[orderFromLocation[col][row - 1]]
                        = shadowRenderTargetSingle[orderFromLocation[col][row]];
                }
                shadowRenderTargetSingle[orderFromLocation[col][Nouter - 1]] = tempTarget;
                needToDrawRectangle[orderFromLocation[col][Nouter - 1]] = true;
            }
        }

        /// <summary>
        /// Set the scale and offset such that the visible subblocks fit perfectly on the (parent) draw Area
        /// </summary>
        private void SetNativeScales()
        {
            // the amount of pixels/meter is Nsampling larger for the whole inner region than in the parent area.
            shadowScale = Nsampling * Scale;

            // offsetX is the left-world location of the parent area, so also WorldX
            // in the shadow area, it is the same world location
            // But in pixels it is given by pixelX = Nborder * blockW.
            // pixelX = shadowScale * (offsetX - shadowOffsetX)
            shadowOffsetX = OffsetX - Nborder * blockW / shadowScale;
            shadowOffsetZ = OffsetZ - Nborder * blockH / shadowScale;

        }

        /// <summary>
        /// Render one of the subblocks (number given by rectangleToDraw)
        /// </summary>
        /// <param name="backgroundColor">The background color to initialize the texture</param>
        /// <param name="drawRoutine">The actual drawing routine delegate</param>
        private void RenderASingleBlockTexture(Color backgroundColor, DrawingDelegate drawRoutine)
        {
            // we wrap this in a try, because we have seen that in some conditions this might crash otherwise
            // Crashes seem to be related to the interaction between XNA and our program: our routines might be
            // called at wrong time for some reason. Possibly XNA simply interrupts a long 'Draw'-call to do an
            // update in between. As a result, spritebatch state might be wrong, and we crash.
            // Very difficult to track and see what is really going on. Therefore: simple try and catch.
            try
            {
                //drawing area depends on rectangle we want to draw
                shadowDrawArea.Scale = shadowScale;
                shadowDrawArea.OffsetX = shadowOffsetX + xOrder[nextRectangleToDraw] * blockW / shadowScale;
                shadowDrawArea.OffsetZ = shadowOffsetZ + zOrder[nextRectangleToDraw] * blockH / shadowScale;
                shadowDrawArea.Update();

                // Rendering the tracks to a single texture
                graphicsDevice.SetRenderTarget(shadowRenderTargetSingle[nextRectangleToDraw]);
                graphicsDevice.Clear(backgroundColor);
                spriteBatch.Begin();
                drawRoutine(shadowDrawArea);
                //shadowDrawArea.DrawBorder(Color.Black);  //debug
                spriteBatch.End();
                graphicsDevice.SetRenderTarget(null);
                shadowMapsSingle[nextRectangleToDraw] = shadowRenderTargetSingle[nextRectangleToDraw];

                needToDrawRectangle[nextRectangleToDraw] = false;
            }
            catch
            {
                graphicsDevice.SetRenderTarget(null); // return control to main render target
            }
            graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Render the single block textures to the combined texture
        /// </summary>
        /// <param name="backgroundColor"></param>
        private void RenderCombinedTexture(Color backgroundColor)
        {
            try
            {   // possibly, during rescaling of the screen, this is called while the size of the combined 
                // texture is (still) larger than the complete screen. In that case we want to draw something
                // that is larger than the graphics device. This will give an error, which should not be there
                // after rescaling has been done properly.
                graphicsDevice.SetRenderTarget(shadowRenderTargetCombined);
                graphicsDevice.Clear(backgroundColor);
                spriteBatch.Begin();
                for (int i = 0; i < Nsubblocks; i++)
                {
                    Vector2 position = new Vector2(xOrder[i] * blockW, (Nouter - 1 - zOrder[i]) * blockH);
                    spriteBatch.Draw(shadowMapsSingle[i], position, null, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
                }
                spriteBatch.End();
                graphicsDevice.SetRenderTarget(null);
                shadowMapCombined = shadowRenderTargetCombined;
            }
            catch
            {
                graphicsDevice.SetRenderTarget(null); // return control to main render target
            }
        }

        /// <summary>
        /// Draw the combined shadow texture to the screen. So this is the routine where all the cached results
        /// are actually drawn to the spriteBatch and you see the result.
        /// </summary>
        public void DrawShadowedTextures()
        {
            // We need to find where, in the combined texture, the lower-left corner of the main area is.
            // The world location we are looking for is worldX = offsetX
            // The main equation is: pixelX = shadowScale * ( worldX - shadowOffsetX)
            // and this gives us the location within the texture immediately!
            //
            // for Z we do it a bit different, because screen drawing is done from the top.
            //          areaH     - areaZ  = scale       * ( worldZ - offsetZ)
            //      blockH*Nouter - pixelZ = shadowScale * ( worldZ - shadowOffsetZ)
            // so
            //      pixelZ = blockH*Nouter - shadowscale * ((areaH - areaZ)/scale +  offsetZ - shadowOffsetZ)
            //
            // For the width: 
            //  in the main area, we have    areaW = scale * worldW
            //  in the shadow map we have    pixelW = shadowScale * worldW.
            //  This leads to pixelW = areaW * shadowScale / scale

            if (shadowMapCombined == null) return; // something unforseen happened

            float scaleRatio = (float)(Scale / shadowScale);
            Vector2 scaleAsVector = new Vector2(scaleRatio);
            Microsoft.Xna.Framework.Rectangle? sourceRectangle = new Microsoft.Xna.Framework.Rectangle(
                Convert.ToInt32(shadowScale * (OffsetX - shadowOffsetX)),
                Convert.ToInt32(blockH * Nouter - shadowScale * (AreaH / Scale + OffsetZ - shadowOffsetZ)),
                Convert.ToInt32(AreaW * shadowScale / Scale),
                Convert.ToInt32(AreaH * shadowScale / Scale));


            Vector2 position = new Vector2(AreaOffsetX, AreaOffsetY);
            Vector2 origin = Vector2.Zero;

            spriteBatch.Draw(shadowMapCombined, position, sourceRectangle, Color.White, 0, origin, scaleAsVector, SpriteEffects.None, 0);

            //debug
            //Console.WriteLine("debug");
            //position = new Vector2(30, 30);
            //sourceRectangle = null;
            //spriteBatch.Draw(shadowMapCombined, position, sourceRectangle, Color.White, 0, origin, scaleAsVector, SpriteEffects.None, 0);
            //spriteBatch.Draw(shadowMapsSingle[2], position, null, Color.White, 0, origin, Vector2.One, SpriteEffects.None, 0);
        }

        #region IDisposable
        private bool disposed;
        /// <summary>
        /// Implementing IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    shadowRenderTargetCombined.Dispose();
                    foreach (RenderTarget2D target in shadowRenderTargetSingle)
                    {
                        target.Dispose();
                    }
                    if (shadowDrawArea != null)
                    {
                        shadowDrawArea.Dispose();
                    }
                    // Dispose managed resources.
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }
        #endregion
    }
}
