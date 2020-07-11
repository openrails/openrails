// COPYRIGHT 2014, 2018 by the Open Rails project.
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

// In this class we gather all the routines to define the area we draw upon.
// Area here is the world-region of ORTS that is visible on the screen
// All zooming and translation code is here, as well as the translation from world coordinates to
// area coordinates and the other way
//
// We have three different coordinates
//  World location: location in the Train Simulator World (often given by tileX, tileZ, x, y, z)
//  Area location: location in pixels relative to upper-left corner of the drawing area
//  Window location: location in pixels relative to the upper-left corner of the actual (parent) window
// Note: screen (area and window) coordinates are given by x, y (from left to right, and top to bottom), 
// whereas world locations are in x, z (from left to right but from bottom to top).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using ORTS.TrackViewer.Properties;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// This is the class that does the translation between world-coordinates and screen coordinates
    /// This also means that it contains all routines for zooming and shifting the drawing area.
    /// Zooming is done using a discrete scale, that means that not all possible numbers are allowed for zooming,
    /// but only a limited amount that give nice round numbers when printing.
    /// This class also has methods to draw lines, arc, textures where the location can be given in world-coordinates.
    /// Here the translation is then done to screen coordinates. The actual drawing is deferred to another class.
    /// During the translation also culling is used: when the shape is (too far) outside the drawing area (in real world coordinates)
    /// the drawing will not take place.
    /// </summary>
    /// <remarks>
    /// In principle the culling will be done also within the actual drawing class, but the current implementation
    /// prevents the long calculation done in especially drawing arcs, it prevents lots of spritebatch calls
    /// and it also works for the situation where only a limited part of the actual screen is used for drawing (e.g. for the inset).
    /// </remarks>
    public class DrawArea
    {
        #region public properties
        // we need doubles instead of floats for accuracy when zoomed in
        // Actually we get rid of this when we split offsetX into offsetTileX and offsetX.
        // but this complicates some math
        /// <summary>scale  (from world size to pixels, so in pixels/meter)</summary>
        public double Scale { get; set; }
        /// <summary>scale at maximum window.</summary>
        /// <summary>WorldLocation of the mouse (so where in the real worlds is the current mouse position</summary>
        public WorldLocation MouseLocation { get; set; }
        /// <summary>location of Upper Left point in real world coordinates</summary>
        public WorldLocation LocationUpperLeft { get; private set; }
        /// <summary>location of Upper Left point in real world coordinates</summary>
        public WorldLocation LocationLowerRight { get; private set; }
        /// <summary>do we check out-of bounds strictly or not.</summary>
        public bool StrictChecking { get; set; }
        #endregion

        #region protected properties
        /// <summary>world-location X corresponding to left side of drawing area</summary>
        protected double OffsetX { get; set; }
        /// <summary>// world-location Z correspoding to bottom of the drawing area</summary>
        protected double OffsetZ { get; set; }
        /// <summary>X location of the top-left corner of this drawArea </summary>
        protected int AreaOffsetX { get; set; }
        /// <summary>Y location of the top-left corner of this drawArea</summary>
        protected int AreaOffsetY { get; set; }
        /// <summary> width of the drawArea in pixels</summary>
        protected int AreaW { get; private set; }
        /// <summary> height of the drawArea in pixels</summary>
        protected int AreaH { get; private set; }

        #endregion

        #region private properties
        private double FullScale { get; set; }
        /// <summary>Ratio used for shifting (percentage shift per update)</summary>
        private static float shiftRatioDefault = 0.10f;

        /// <summary> Zooming might affect also other things. In this case we only support drawScaleRuler
        /// a better implementation would use delegates to deal with this</summary>
        private DrawScaleRuler drawScaleRuler;

        /// <summary>The discrete scale giving the meters per pixel in nice numbers</summary>
        private DiscreteScale metersPerPixel;

        /// <summary>The fontmanager that is used to draw strings</summary>
        private FontManager fontManager;
        #endregion

        #region General public methods
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="drawScaleRuler">The Scale ruler that needs to be updated on screen and zoom sizes</param>
        public DrawArea(DrawScaleRuler drawScaleRuler)
        {
            AreaW = 1230;
            AreaH = 700;
            metersPerPixel = new DiscreteScale();
            MouseLocation = new WorldLocation(0, 0, 0, 0, 0);  // default mouse location far far away
            fontManager = FontManager.Instance;
            SetDrawArea(-1, 1, -1, 1); // just have a default
            this.drawScaleRuler = drawScaleRuler;
        }

        /// <summary>
        /// Sets the screen size on which we can draw (in pixels)
        /// </summary>
        /// <param name="areaOffsetX">cornerIndexX-position of the top-left corner of the draw-area, in pixels</param>
        /// <param name="areaOffsetY">y-position of the top-left corner of the draw-area, in pixels</param>
        /// <param name="areaWidth">width of the area to draw upon, in pixels</param>
        /// <param name="areaHeight">height of the area to draw upon, in pixels</param>
        public virtual void SetScreenSize(int areaOffsetX, int areaOffsetY, int areaWidth, int areaHeight)
        {
            // we also need to reset the fullscale;
            double fullScaleX = FullScale * areaWidth / this.AreaW;
            double fullScaleY = FullScale * areaHeight / this.AreaH;
            FullScale = Math.Min(fullScaleX, fullScaleY);
            this.AreaOffsetX = areaOffsetX;
            this.AreaOffsetY = areaOffsetY;
            this.AreaW = areaWidth;
            this.AreaH = areaHeight;
        }

        /// <summary>
        /// Save the zoom to persistent settings
        /// </summary>
        /// <param name="routePath">The string describing the route Path to identify the zoom</param>
        public void Save(string routePath)
        {
            Properties.Settings.Default.zoomOffsetX = OffsetX;
            Properties.Settings.Default.zoomOffsetZ = OffsetZ;
            Properties.Settings.Default.zoomScale = Scale;
            Properties.Settings.Default.zoomRoutePath = routePath;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Restore the zoom from persistent settings
        /// </summary>
        public void Restore()
        {
            if (Properties.Settings.Default.zoomScale > -1)
            {
                OffsetX = Properties.Settings.Default.zoomOffsetX;
                OffsetZ = Properties.Settings.Default.zoomOffsetZ;
                metersPerPixel.ApproximateTo(1.0f / Properties.Settings.Default.zoomScale);
                Scale = metersPerPixel.InverseScaleValue;
                PostZoomTasks(true);
            }
        }

        /// <summary>
        /// Update whatever needs updating. Currently only mouseLocation and boundary tiles
        /// </summary>
        public void Update()
        {
            // find the grids that are actually drawn.
            LocationUpperLeft = GetWorldLocation(0, 0);
            LocationLowerRight = GetWorldLocation(AreaW, AreaH);

            //mouse location is given in parent window location, so correct offset
            MouseLocation = GetWorldLocation(UserInterface.TVUserInput.MouseLocationX - AreaOffsetX,
                                             UserInterface.TVUserInput.MouseLocationY - AreaOffsetY);
        }

        /// <summary>
        /// Reset zoom area to show complete track
        /// </summary>
        /// <param name="drawTrackDB">Track database used to determine what the complete track area is</param>
        public void ZoomReset(DrawTrackDB drawTrackDB)
        {
            if (drawTrackDB == null) return;
            float minX = drawTrackDB.MinTileX * 2048 - 1024f;
            float minZ = drawTrackDB.MinTileZ * 2048 - 1024f;
            float maxX = drawTrackDB.MaxTileX * 2048 + 1024f;
            float maxZ = drawTrackDB.MaxTileZ * 2048 + 1024f;
            SetDrawArea(minX, maxX, minZ, maxZ);
        }

        /// <summary>
        /// Intitialize the draw area from min and max cornerIndexX and cornerIndexZ in world locations
        /// </summary>
        /// <param name="minX">minimal real world X location</param>
        /// <param name="maxX">maximal real world X location</param>
        /// <param name="minZ">minimal real world Z location</param>
        /// <param name="maxZ">maximal real world Z location</param>
        void SetDrawArea(float minX, float maxX, float minZ, float maxZ)
        {
            float scaleX = AreaW / (maxX - minX);
            float scaleY = AreaH / (maxZ - minZ);
            //make square tiles
            Scale = Math.Min(scaleX, scaleY);
            metersPerPixel.ApproximateTo(1.0f / Scale);
            Scale = metersPerPixel.InverseScaleValue;
            FullScale = Scale;
            // center the window. Equation we use is areaX = scale * (worldX - offsetX)
            // or offsetX = worldX - areaX/scale. 
            OffsetX = (maxX + minX) / 2 - AreaW / 2 / Scale;
            OffsetZ = (maxZ + minZ) / 2 - AreaH / 2 / Scale;
            PostZoomTasks(true);
        }

        /// <summary>
        /// Check whether a given bool is outside the drawing area (or not).
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>Boolean describing describing whether the point is out</returns>
        public bool OutOfArea(WorldLocation point)
        {
            Vector2 areaVector = GetAreaVector(point);
            float leeway = (StrictChecking) ? 0 : AreaW;
            if (areaVector.X < -leeway) return true;
            if (areaVector.Y < -leeway) return true;
            if (areaVector.X > AreaW + leeway) return true;
            if (areaVector.Y > AreaH + leeway) return true;
            return false;
        }
        #endregion

        #region Zooming
        /// <summary>
        /// Zoom around the location given by the preference
        /// </summary>
        /// <param name="scaleSteps">The amount of zoom-steps to take (using the discrete scale)</param>
        public void Zoom(int scaleSteps)
        {
            if (Properties.Settings.Default.zoomIsCenteredOnMouse)
            {
                ZoomAroundMouse(scaleSteps);
            }
            else
            {
                ZoomCentered(scaleSteps);
            }
        }

        /// <summary>
        /// Do a single zoom step around the mouse location
        /// </summary>
        /// <param name="scaleSteps">The amount of zoom-steps to take (using the discrete scale). Positive means zooming out</param>
        private void ZoomAroundMouse(int scaleSteps)
        {
            ZoomAround(GetAreaVector(MouseLocation), scaleSteps);
        }

        /// <summary>
        /// Do a single zoom step around the center of the drawing area.
        /// </summary>
        /// <param name="scaleSteps">The amount of zoom-steps to take (using the discrete scale)</param>
        public void ZoomCentered(int scaleSteps)
        {
            ZoomAround(new Vector2(AreaW / 2, AreaH / 2), scaleSteps);
        }

        /// <summary>
        /// Do a single zoom-in step around a certain fixed position.
        /// This is the routine where the actually zooming happens.
        /// </summary>
        /// <param name="fixedAreaLocation">cornerIndexX- and y-coordinates (in pixels) of the fixed position</param>
        /// <param name="scaleSteps">The amount of zoom-steps to take (using the discrete scale)</param>
        private void ZoomAround(Vector2 fixedAreaLocation, int scaleSteps)
        {
            if (scaleSteps > 0)
            {
                // prevent too much zooming out, scale is in pixels/meter
                double minScale = FullScale / 2;
                double maxRatio = Scale / minScale;
                int maxSteps = metersPerPixel.StepsNeededForRatio(maxRatio);
                if (maxSteps <= 0)
                {
                    return;
                }
                scaleSteps = Math.Min(scaleSteps, maxSteps);
            }


            // equation is areaX = scale * (worldX - offsetX)
            // for a fixed point (in area location) we have
            //  fixedX = scale_old * (worldX - offsetX_old) = scale_new * (worldX - offsetX_new)
            //  fixedX/scale_old + offsetX_old = fixedX/scale_new + offsetX_new = worldX
            //  offsetX_new = offsetX_old + fixedX * (scale_new/scale_old - 1) / scale_new
            float scaleFactor = 1.0f / (float)metersPerPixel.AddStep(scaleSteps); // 1.0/xxx because scale is inverse of metersPerPixel
            Scale = metersPerPixel.InverseScaleValue;
            OffsetX += fixedAreaLocation.X * (scaleFactor - 1) / Scale;
            OffsetZ += (AreaH - fixedAreaLocation.Y) * (scaleFactor - 1) / Scale;
            PostZoomTasks(true);
        }

        /// <summary>
        /// Zoom to such a scale that a single tile will fit (in either X or Z-direction)
        /// </summary>
        public void ZoomToTile()
        {
            //normal equation: areaX = scale * (worldX - offsetX)
            //zoom to tile: screenW = scale_new * 2048
            double scaleX = AreaW / 2048.0;
            double scaleY = AreaH / 2048.0;
            double newScale = Math.Min(scaleX, scaleY);
            int stepsNeeded = metersPerPixel.StepsNeededForRatio(Scale / newScale);
            ZoomAroundMouse(stepsNeeded);
        }

        /// <summary>
        /// Zoom to such a scale that a single tile will fit, center of screen
        /// </summary>
        public void ZoomToTileCentered()
        {
            double scaleX = AreaW / 2048.0;
            double scaleY = AreaH / 2048.0;
            double newScale = Math.Min(scaleX, scaleY);
            ZoomCentered(metersPerPixel.StepsNeededForRatio(Scale / newScale));
        }

        /// <summary>
        /// Do the various needed tasks after a zoom: mainly updating related objects
        /// </summary>
        /// <param name="updateFontSize">do we want to update the </param>
        private void PostZoomTasks(bool updateFontSize)
        {
            if (drawScaleRuler != null) drawScaleRuler.SetCurrentRuler(Scale);

            if (updateFontSize)
            {
                //Set the size of the font for drawing text. Font size is about 10 times the scale in pixels/meter
                fontManager.RequestFontSize((int)(Scale * 10));
            }
        }
        #endregion

        #region Shifting
        /// <summary>
        /// Adjust the scale etc to follow another drawArea, showing either the full scale or at max a scale
        /// that is maxScaleRatio difference
        /// </summary>
        /// <param name="otherArea">The other drawArea that needs to be followed</param>
        /// <param name="maxScaleRatio">Maximal allowed ratio between this scale and the scale of the otherArea</param>
        public void Follow(DrawArea otherArea, float maxScaleRatio)
        {
            double otherScale = otherArea.Scale;
            double otherScaleCorrected = otherScale * this.AreaW / otherArea.AreaW; // Correct for different screen size
            Scale = Math.Max(FullScale, otherScaleCorrected / maxScaleRatio);
            // center on the same center as otherArea
            // other.screenW/2 = otherScale * (WorldX - other.offsetX)
            // this.screenW/2  = this.Scale * (WorldX - this.offsetX);
            // so WorldX = other.offsetX + other.screenW/2 / otherScale
            this.OffsetX = otherArea.OffsetX + otherArea.AreaW / 2 / otherArea.Scale - this.AreaW / 2 / this.Scale;
            this.OffsetZ = otherArea.OffsetZ + otherArea.AreaH / 2 / otherArea.Scale - this.AreaH / 2 / this.Scale;
            MouseLocation = otherArea.MouseLocation;
            PostZoomTasks(false);
        }

        /// <summary>
        /// Shift the view window to the left
        /// </summary>
        public void ShiftLeft()
        {
            OffsetX -= shiftRatioDefault * AreaW / Scale / 2;
        }

        /// <summary>
        /// Shift the view window to the right
        /// </summary>
        public void ShiftRight()
        {
            OffsetX += shiftRatioDefault * AreaW / Scale / 2;
        }

        /// <summary>
        /// Shift the view window up
        /// </summary>
        public void ShiftUp()
        {
            OffsetZ += shiftRatioDefault * AreaH / Scale / 2;
        }

        /// <summary>
        /// Shift the view window down
        /// </summary>
        public void ShiftDown()
        {
            OffsetZ -= shiftRatioDefault * AreaH / Scale / 2;
        }

        /// <summary>
        /// Shift the view window with a number of pixels (e.g. to follow a mouse)
        /// </summary>
        /// <param name="pixelsX">Number of pixels to shift in X direction</param>
        /// <param name="pixelsY">Number of pixels to shift in Y direction</param>
        public void ShiftArea(int pixelsX, int pixelsY)
        {
            OffsetX -= pixelsX / Scale;
            OffsetZ += pixelsY / Scale;
        }

        /// <summary>
        /// Shift the window to have the given world location at its center
        /// </summary>
        /// <param name="location">New worldLocation at center of area</param>
        public void ShiftToLocation(WorldLocation location)
        {
            if (location == WorldLocation.None) return;
            // Basic equation areaX = scale * (worldX - offsetX)
            // We want middle of screen to shift to new worldX, so areaW/2 = scale * (worldX - offsetX)
            // Similarly
            double worldX = location.TileX * 2048 + location.Location.X;
            double worldZ = location.TileZ * 2048 + location.Location.Z;
            OffsetX = worldX - AreaW / (2 * Scale);
            OffsetZ = worldZ - AreaH / (2 * Scale);
        }
        #endregion

        #region Internal translation routines
        /// <summary>
        /// Translate world sizes to screen pixels (rounded up to make sure something is drawn).
        /// Note, size in area and size in window are the same.
        /// </summary>
        /// <param name="worldSize">size in the real world in meters</param>
        /// <returns>size in window pixels</returns>
        private float GetWindowSize(float worldSize)
        {
            return (float)Math.Ceiling(Scale * worldSize);
        }

        /// <summary>
        /// Translate a WorldLocation to an area location
        /// </summary>
        /// <param name="location">location in World coordinates (including tiles)</param>
        /// <returns>location on the drawing area in a 2d vector (in pixels)</returns>
        private Vector2 GetAreaVector(WorldLocation location)
        {
            double x = location.TileX * 2048 + location.Location.X;
            double y = location.TileZ * 2048 + location.Location.Z;
            return new Vector2((float)(Scale * (x - OffsetX)),
                               (float)(AreaH - Scale * (y - OffsetZ)));
        }

        /// <summary>
        /// Translate a WorldLocation to parent window coordinates
        /// </summary>
        /// <param name="location">location in World coordinates (including tiles)</param>
        /// <returns>location on the parent window in a 2d vector (in pixels)</returns>
        private Vector2 GetWindowVector(WorldLocation location)
        {
            Vector2 windowVector = GetAreaVector(location);
            windowVector.X += AreaOffsetX;
            windowVector.Y += AreaOffsetY;
            return windowVector;
        }

        /// <summary>
        /// Translate a area location into a parent window location
        /// </summary>
        /// <param name="areaX">X-location (in pixels relative to drawing area)</param>
        /// <param name="areaY">Y-location (in pixels relative to drawing area)</param>
        /// <returns>location on the parent window in a 2d vector</returns>
        private Vector2 GetWindowVector(int areaX, int areaY)
        {
            return new Vector2(areaX + AreaOffsetX, areaY + AreaOffsetY);
        }

        /// <summary>
        /// Calculate back the World location corresponding to given area coordinate
        /// </summary>
        /// <param name="areaX">X-coordinate on the area (in pixels)</param>
        /// <param name="areaY">Y-coordinate on the area (in pixels)</param>
        /// <returns>Corresponding world location</returns>
        private WorldLocation GetWorldLocation(int areaX, int areaY)
        {
            double x = (OffsetX + (areaX) / Scale);
            double z = (OffsetZ + (AreaH - areaY) / Scale);
            //WorldLocation location = new WorldLocation(0, 0, cornerIndexX, 0, cornerIndexZ);
            //we now do pre-normalization. This normalization is more efficient than Coordinates.normalization
            int tileX = (int)x / 2048;
            x -= (tileX * 2048);
            int tileZ = (int)z / 2048;
            z -= tileZ * 2048;
            WorldLocation location = new WorldLocation(tileX, tileZ, (float)x, 0, (float)z);
            location.Normalize();
            return location;
        }
        #endregion

        #region Draw objects like lines
        /// <summary>
        /// Basic method to draw a line between two points. Coordinates are in area coordinates.
        /// </summary>
        /// <param name="width"> Width of the line to draw in meters </param>
        /// <param name="color"> Color of the line</param>
        /// <param name="point1"> WorldLocation of the first point of the line</param>
        /// <param name="point2"> WorldLocation of to the last point of the line</param>
        public void DrawLine(float width, Color color, WorldLocation point1, WorldLocation point2)
        {
            if (OutOfArea(point1) && OutOfArea(point2)) return;
            BasicShapes.DrawLine(GetWindowSize(width), color, GetWindowVector(point1), GetWindowVector(point2));
        }

        /// <summary>
        /// Basic method to draw a dashed line between two points. Coordinates are in area coordinates.
        /// </summary>
        /// <param name="width"> Width of the line to draw in meters </param>
        /// <param name="color"> Color of the line</param>
        /// <param name="point1"> WorldLocation of the first point of the line</param>
        /// <param name="point2"> WorldLocation of to the last point of the line</param>
        public void DrawDashedLine(float width, Color color, WorldLocation point1, WorldLocation point2)
        {
            if (OutOfArea(point1) && OutOfArea(point2)) return;
            BasicShapes.DrawDashedLine(GetWindowSize(width), color, GetWindowVector(point1), GetWindowVector(point2));
        }

        /// <summary>
        /// Basic method to draw a line between two points without checking whether it is out of bounds or not. Coordinates are in area coordinates.
        /// </summary>
        /// <param name="width"> Width of the line to draw in meters </param>
        /// <param name="color"> Color of the line</param>
        /// <param name="point1"> WorldLocation of the first point of the line</param>
        /// <param name="point2"> WorldLocation of to the last point of the line</param>
        public void DrawLineAlways(float width, Color color, WorldLocation point1, WorldLocation point2)
        {
            BasicShapes.DrawLine(GetWindowSize(width), color, GetWindowVector(point1), GetWindowVector(point2));
        }

        /// <summary>
        /// Basic method to draw a line starting at a point between two points. 
        /// Parameters are in world coordinates and sizes.
        /// </summary>
        /// <param name="width"> Width of the line to draw in meters </param>
        /// <param name="color"> Color of the line</param>
        /// <param name="point"> WorldLocation of the first point of the line (for zero offset)</param>
        /// <param name="length"> length of the line to draw in meters (also when shifted by offset)</param>
        /// <param name="angle"> Angle (in rad east of North)</param>
        /// <param name="lengthOffset">Instead of starting to draw at the given point, only start to draw a distance offset further along the line</param>
        public void DrawLine(float width, Color color, WorldLocation point, float length, float angle, float lengthOffset)
        {
            WorldLocation beginPoint;
            float sinAngle = (float)Math.Sin(angle);
            float cosAngle = (float)Math.Cos(angle);
            if (lengthOffset == 0)
            {
                beginPoint = point;
            }
            else
            {
                beginPoint = new WorldLocation(point);
                beginPoint.Location.X += lengthOffset * sinAngle;
                beginPoint.Location.Z += lengthOffset * cosAngle;
            }
            WorldLocation endPoint = new WorldLocation(beginPoint); //location of end-point
            endPoint.Location.X += length * sinAngle;
            endPoint.Location.Z += length * cosAngle;
            if (OutOfArea(beginPoint) && OutOfArea(endPoint)) return;
            // definition of rotation in ORTS is angle right of North
            // rotation in the window/draw area is angle right/south of right-horizontal
            // hence a 90 degree correction
            // To prevent double calculations, offset is already taken into account here
            BasicShapes.DrawLine(GetWindowSize(width), color, GetWindowVector(beginPoint),
                GetWindowSize(length), angle - MathHelper.Pi / 2);
        }

        /// <summary>
        /// Basic method to draw an arc, intended to be used for drawing a curver track section
        /// Parameters are in world coordinates and sizes 
        /// </summary>
        /// <param name="width">Width of the arc-line to draw in meters</param>
        /// <param name="color">Color of the arc-line</param>
        /// <param name="point">WorldLocation of the starting point of the arc</param>
        /// <param name="radius">Radius of the curvature of the arc</param>
        /// <param name="angle">Angle (in degrees east of North) of the first part of the arc</param>
        /// <param name="arcDegrees">Number of degrees in the arc (360 would be full circle)</param>
        /// <param name="arcDegreesOffset">Offset of the number of degrees (meaning, do not draw the first arcDegreesOffset degrees</param>
        public void DrawArc(float width, Color color, WorldLocation point, float radius, float angle, float arcDegrees, float arcDegreesOffset)
        {
            WorldLocation beginPoint; // (possibly approximate) location of begin-point
            if (arcDegreesOffset == 0)
            {
                beginPoint = point;
            }
            else
            {
                beginPoint = new WorldLocation(point);
                float arcRadOffset = arcDegreesOffset * MathHelper.Pi / 180;
                float lengthOffset = radius * arcRadOffset;
                beginPoint.Location.X += lengthOffset * (float)Math.Sin(angle + arcRadOffset / 2);
                beginPoint.Location.Z += lengthOffset * (float)Math.Cos(angle + arcRadOffset / 2);
            }
            WorldLocation endPoint = new WorldLocation(beginPoint); //approximate location of end-point
            float arcRad = arcDegrees * MathHelper.Pi / 180;
            float length = radius * arcRad;
            endPoint.Location.X += length * (float)Math.Sin(angle + arcRad / 2);
            endPoint.Location.Z += length * (float)Math.Cos(angle + arcRad / 2);
            if (OutOfArea(beginPoint) && OutOfArea(endPoint)) return;
            // for the 90 degree offset, see DrawLine
            BasicShapes.DrawArc(GetWindowSize(width), color, GetWindowVector(point),
                GetWindowSize(radius), angle - MathHelper.Pi / 2, arcDegrees, arcDegreesOffset);
        }

        /// <summary>
        /// Draw a vertical line trough the given world line, all the way from bottom to top. 1 pixel wide.
        /// </summary>
        /// <param name="color">Color of the line</param>
        /// <param name="point">Worldlocation through which to draw the line</param>
        private void DrawLineVertical(Color color, WorldLocation point)
        {
            Vector2 middle = GetWindowVector(point);
            Vector2 top = GetWindowVector(0, 0);
            Vector2 bot = GetWindowVector(0, AreaH);
            bot.X = middle.X;
            top.X = middle.X;
            BasicShapes.DrawLine(1, color, bot, top);
        }

        /// <summary>
        /// Draw a horizontal line trough the given world line, all the way from bottom to top. 1 pixel wide.
        /// </summary>
        /// <param name="color">Color of the line</param>
        /// <param name="point">Worldlocation through which to draw the line</param>
        private void DrawLineHorizontal(Color color, WorldLocation point)
        {
            Vector2 middle = GetWindowVector(point);
            Vector2 left = GetWindowVector(0, 0);
            Vector2 right = GetWindowVector(AreaW, 0);
            right.Y = middle.Y;
            left.Y = middle.Y;
            BasicShapes.DrawLine(1, color, left, right);
        }

        /// <summary>
        /// Draw the grid consisting of the boundaries of the WorldTiles within the drawing area
        /// </summary>
        public void DrawTileGrid()
        {
            if (!Properties.Settings.Default.showGridLines)
            {
                return;
            }
            // draw tile Grid boundaries. Note that coordinates within tile are from -1024 to 1024
            for (int tileX = LocationUpperLeft.TileX; tileX <= LocationLowerRight.TileX + 1; tileX++)
            {
                DrawLineVertical(Color.Gray, new WorldLocation(tileX, 0, -1024f, 0f, 0f));
            }
            for (int tileZ = LocationLowerRight.TileZ; tileZ <= LocationUpperLeft.TileZ + 1; tileZ++)
            {
                DrawLineHorizontal(Color.Gray, new WorldLocation(0, tileZ, 0f, 0f, -1024f));
            }

        }

        /// <summary>
        /// Simply draw a blank background for this area
        /// </summary>
        /// <param name="color">The background color you want</param>
        public void DrawBackground(Color color)
        {
            BasicShapes.DrawLine(AreaW, color, GetWindowVector(AreaW / 2, 0), GetWindowVector(AreaW / 2, AreaH));
        }

        /// <summary>
        /// Draw a border around the area
        /// </summary>
        /// <param name="color">The color of the border line</param>
        public void DrawBorder(Color color)
        {
            // We do not use DrawBorder below to prevent a line not being drawn because of OutOfArea
            BasicShapes.DrawLine(1, color, GetWindowVector(0, 0), GetWindowVector(0, AreaH));
            BasicShapes.DrawLine(1, color, GetWindowVector(AreaW, 0), GetWindowVector(AreaW, AreaH));
            BasicShapes.DrawLine(1, color, GetWindowVector(0, 0), GetWindowVector(AreaW, 0));
            BasicShapes.DrawLine(1, color, GetWindowVector(0, AreaH), GetWindowVector(AreaW, AreaH));
        }

        /// <summary>
        /// Draw the border (in world coordinates) of another area (e.g. showing a zoom window).
        /// </summary>
        /// <param name="color">The color of the border line</param>
        /// <param name="otherDrawArea">The other area of which the border needs to be drawn</param>
        public void DrawBorder(Color color, DrawArea otherDrawArea)
        {
            WorldLocation locationUpperLeft = otherDrawArea.GetWorldLocation(0, 0);
            WorldLocation locationUpperRight = otherDrawArea.GetWorldLocation(otherDrawArea.AreaW, 0);
            WorldLocation locationLowerLeft = otherDrawArea.GetWorldLocation(0, otherDrawArea.AreaH);
            WorldLocation locationLowerRight = otherDrawArea.GetWorldLocation(otherDrawArea.AreaW, otherDrawArea.AreaH);

            DrawLine(1, color, locationUpperLeft, locationUpperRight);
            DrawLine(1, color, locationUpperLeft, locationLowerLeft);
            DrawLine(1, color, locationLowerLeft, locationLowerRight);
            DrawLine(1, color, locationUpperRight, locationLowerRight);

        }

        /// <summary>
        /// Draw/print a string message on the draw area
        /// </summary>
        /// <param name="location">The world location acting as the starting point of the drawing</param>
        /// <param name="message">The message to print</param>
        public void DrawExpandingString(WorldLocation location, string message)
        {
            // We offset the top-left corner to make sure the text is not on the marker.
            int offsetXY = 2 + (int)GetWindowSize(2f);
            DrawExpandingString(location, message, offsetXY, offsetXY);
        }

        /// <summary>
        /// Draw/print a string message on the draw area
        /// </summary>
        /// <param name="location">The world location acting as the starting point of the drawing</param>
        /// <param name="message">The message to print</param>
        /// <param name="offsetX">The offset in X-direction of the top-left location in pixels</param>
        /// <param name="offsetY">The offset in Y-direction of the top-left location in pixels</param>
        public void DrawExpandingString(WorldLocation location, string message, int offsetX, int offsetY)
        {
            if (OutOfArea(location)) return;
            Vector2 textOffset = new Vector2(offsetX, offsetY);
            BasicShapes.DrawExpandingString(GetWindowVector(location) + textOffset, DrawColors.colorsNormal.Text, message);
        }
        #endregion

        #region Draw 2D textures
        /// <summary>
        /// Draw a texture, determined by its name.
        /// </summary>
        /// <param name="location">Location where to draw the texture</param>
        /// <param name="textureName">Name identifying the texture</param>
        /// <param name="size">Size of the texture in world-meters</param>
        /// <param name="minPixelSize">Minimum size in pixels, to make sure you always see something</param>
        public void DrawTexture(WorldLocation location, string textureName, float size, int minPixelSize)
        {
            DrawTexture(location, textureName, size, minPixelSize, Color.White, 0);
        }

        /// <summary>
        /// Draw a texture, determined by its name.
        /// </summary>
        /// <param name="location">Location where to draw the texture</param>
        /// <param name="textureName">Name identifying the texture</param>
        /// <param name="size">Size of the texture in world-meters</param>
        /// <param name="minPixelSize">Minimum size in pixels, to make sure you always see something</param>
        /// <param name="color">Color you want the simple texture to have</param>
        public void DrawTexture(WorldLocation location, string textureName, float size, int minPixelSize, Color color)
        {
            DrawTexture(location, textureName, size, minPixelSize, color, 0);
        }

        /// <summary>
        /// Draw a texture, determined by its name.
        /// </summary>
        /// <param name="location">Location where to draw the texture</param>
        /// <param name="textureName">Name identifying the texture</param>
        /// <param name="angle">Rotation angle for the texture</param>
        /// <param name="size">Size of the texture in world-meters</param>
        /// <param name="minPixelSize">Minimum size in pixels, to make sure you always see something</param>
        /// <param name="color">Color you want the simple texture to have</param>
        public void DrawTexture(WorldLocation location, string textureName, float size, int minPixelSize, Color color, float angle)
        {
            if (OutOfArea(location)) return;
            float pixelSize = (float)Math.Max(GetWindowSize(size), minPixelSize);
            BasicShapes.DrawTexture(GetWindowVector(location), textureName, angle, pixelSize, color, false);
        }

        /// <summary>
        /// Draw a texture, determined by its name, with possible flipping
        /// </summary>
        /// <param name="location">Location where to draw the texture</param>
        /// <param name="textureName">Name identifying the texture</param>
        /// <param name="angle">Rotation angle for the texture</param>
        /// <param name="size">Size of the texture in world-meters</param>
        ///<param name="flip">Whether the texture needs to be flipped (vertically)</param>
        public void DrawTexture(WorldLocation location, string textureName, float size,  float angle, bool flip)
        {
            if (OutOfArea(location)) return;
            float pixelSize = GetWindowSize(size);
            BasicShapes.DrawTexture(GetWindowVector(location), textureName, angle, pixelSize, Color.White, flip);
        }

        /// <summary>
        /// Draw a texture, determined by its name.
        /// </summary>
        /// <param name="location">Location where to draw the texture</param>
        /// <param name="textureName">Name identifying the texture</param>
        /// <param name="size">Size of the texture in world-meters</param>
        /// <param name="minPixelSize">Minimum size in pixels, to make sure you always see something</param>
        /// <param name="maxPixelSize">Maximum size in pixels, to make sure icons are not becoming too big</param>
        /// <param name="color">Color you want the simple texture to have</param>
        public void DrawTexture(WorldLocation location, string textureName, float size, int minPixelSize, int maxPixelSize, Color color)
        {
            DrawTexture(location, textureName, size, minPixelSize, maxPixelSize, color, 0);
        }

        /// <summary>
        /// Draw a texture, determined by its name.
        /// </summary>
        /// <param name="location">Location where to draw the texture</param>
        /// <param name="textureName">Name identifying the texture</param>
        /// <param name="angle">Rotation angle for the texture</param>
        /// <param name="size">Size of the texture in world-meters</param>
        /// <param name="minPixelSize">Minimum size in pixels, to make sure you always see something</param>
        /// <param name="maxPixelSize">Maximum size in pixels, to make sure icons are not becoming too big</param>
        /// <param name="color">Color you want the simple texture to have</param>
        public void DrawTexture(WorldLocation location, string textureName, float size, int minPixelSize, int maxPixelSize, Color color, float angle)
        {
            if (OutOfArea(location)) return;
            float pixelSize = (float)Math.Min(Math.Max(GetWindowSize(size), minPixelSize), maxPixelSize);
            BasicShapes.DrawTexture(GetWindowVector(location), textureName, angle, pixelSize, color, false);
        }
        #endregion

    }

    #region DiscreteScale
    /// <summary>
    /// Class to model a discrete scale (so not continuous). The idea is that the values of the scale are
    /// nice round numbers for the user, while still trying to have the ratio between a step on the scale
    /// and the next step to be almost constant.
    /// The default scale has 24 steps per decade, each 8 steps being about a ratio of 2, each 4 steps about sqrt(2). 
    /// The actual value of the scale is then (some round number) * 10^(some integer power).
    /// </summary>
    public class DiscreteScale
    {
        /// <summary>The nice and round numbers between 10 and 100 to be used for the stpes</summary>
        private static int[] discreteSteps = { 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 27, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 90 };
        /// <summary>The index giving which round number is to be used</summary>
        private int stepIndex;
        /// <summary>The power of ten to be used</summary>
        private int powerOfTen;
        /// <summary>Limit the maximum zoom allowed</summary>
        private const int minPowerOfTen = -5;

        /// <summary>Return the value of the scale (should be (some round number) * 10^(some integer power)</summary>
        public double ScaleValue { get { return discreteSteps[stepIndex] * Math.Pow(10.0, (double)powerOfTen); } }
        /// <summary>Return 1 divided by the value of the scale</summary>
        public double InverseScaleValue { get { return 1.0 / ScaleValue; } }

        /// <summary>
        /// Increase the scale with given number of discrete steps.
        /// </summary>
        /// <param name="stepsToAdd">Number of steps to add (can be negative)</param>
        /// <returns>The ratio between new value and previous value</returns>
        public double AddStep(int stepsToAdd)
        {
            double previousValue = ScaleValue;
            stepIndex += stepsToAdd;
            while (stepIndex >= discreteSteps.Count())
            {
                stepIndex -= discreteSteps.Count();
                powerOfTen ++;
            }
            while (stepIndex < 0)
            {
                stepIndex += discreteSteps.Count();
                powerOfTen --;
                if (powerOfTen < minPowerOfTen)
                {
                    powerOfTen = minPowerOfTen;
                    stepIndex = 0;
                }
            }
            return ScaleValue / previousValue;
        }

        /// <summary>
        /// Set the scale to a value approximating the requested value (rounding down)
        /// </summary>
        /// <param name="requestedValue">value you want the scale to be close to</param>
        public void ApproximateTo(double requestedValue)
        {
            powerOfTen = Convert.ToInt32(Math.Floor(Math.Log10(requestedValue))) - 1;
            double restValue = requestedValue *  Math.Pow(10 , -(double)powerOfTen);
            for (stepIndex = 0; stepIndex < discreteSteps.Count() && discreteSteps[stepIndex] < restValue; stepIndex++) { };
            if (stepIndex == discreteSteps.Count())
            {   // the value requested is larger than 90. 10^n, which means we get 100.10^n.
                // This needs to be 10.10^(n+1)
                powerOfTen += 1;
                stepIndex = 0;
            }
        }

        /// <summary>
        /// Return the steps it takes to go from the current value to value*ratio. 
        /// </summary>
        /// <param name="ratio">Ratio that needs to be converted to steps. Can be smaller than one</param>
        /// <returns>the number of steps. Will be negative when ratio smaller than 1</returns>
        public int StepsNeededForRatio(double ratio)
        {
            DiscreteScale newScale = new DiscreteScale();
            newScale.ApproximateTo(ratio * ScaleValue);
            int neededSteps = newScale.stepIndex - this.stepIndex;
            neededSteps += discreteSteps.Count() * (newScale.powerOfTen - this.powerOfTen);
            return neededSteps;
        }
    }
    #endregion
}
