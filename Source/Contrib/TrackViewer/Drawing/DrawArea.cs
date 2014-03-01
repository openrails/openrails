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
        // we need doubles instead of floats for accuracy when zoomed in
        // Actually we get rid of this when we split offsetX into offsetTileX and offsetX.
        // but this complicates some math
        private double scale; // scale in X direction (from world size to pixels, so in pixels/meter)
        private double fullScale; // scale at maximum window.
        private double offsetX; // world-location X corresponding to left side of drawing area
        private double offsetZ; // world-location Y correspoding to bottom of the drawing area
        private int areaOffsetX; // location of the top-left corner of this drawArea 
        private int areaOffsetY; // y-location of the top-left corner of this drawArea
        private int areaW = 1230; // width of the drawArea in pixels
        private int areaH = 700; // height of the drawArea in pixels
        private static float shiftRatioDefault = 0.10f; // Ratio used for shifting

        private DrawScaleRuler drawScaleRuler;

        private DiscreteScale metersPerPixel;


        public WorldLocation mouseLocation;
        public bool strictChecking = false;  // do we check out-of bounds strict or not.

        /// <summary>
        /// constructor
        /// </summary>
        public DrawArea(DrawScaleRuler drawScaleRuler)
        {
            metersPerPixel = new DiscreteScale();
            mouseLocation = new WorldLocation(0, 0, 0, 0, 0);  // default mouse location far far away
            setDrawArea(-1, 1, -1, 1); // just have a default
            this.drawScaleRuler = drawScaleRuler;
       }

        /// <summary>
        /// Sets the screen size on which we can draw (in pixels)
        /// </summary>
        /// <param name="areaOffsetX">x-position of the top-left corner of the draw-area, in pixels</param>
        /// <param name="areaOffsetY">y-position of the top-left corner of the draw-area, in pixels</param>
        /// <param name="areaWidth">width of the area to draw upon, in pixels</param>
        /// <param name="areaHeight">height of the area to draw upon, in pixels</param>
        public void setScreenSize(int areaOffsetX, int areaOffsetY, int areaWidth, int areaHeight)
        {
            // we also need to reset the fullscale;
            double fullScaleX = fullScale * areaWidth / this.areaW;
            double fullScaleY = fullScale * areaHeight / this.areaH;
            fullScale = Math.Min(fullScaleX, fullScaleY);
            this.areaOffsetX = areaOffsetX;
            this.areaOffsetY = areaOffsetY;
            this.areaW = areaWidth;
            this.areaH = areaHeight;
        }

        /// <summary>
        /// Save the zoom to persistent settings
        /// </summary>
        /// <param name="routePath">The string describing the route Path to identify the zoom</param>
        public void Save(string routePath)
        {
            Properties.Settings.Default.zoomOffsetX = offsetX;
            Properties.Settings.Default.zoomOffsetZ = offsetZ;
            Properties.Settings.Default.zoomScale = scale;
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
                offsetX = Properties.Settings.Default.zoomOffsetX;
                offsetZ = Properties.Settings.Default.zoomOffsetZ;
                metersPerPixel.ApproximateTo(1.0f / Properties.Settings.Default.zoomScale);
                scale = metersPerPixel.InverseScaleValue;
                postZoomTasks();
            }
        }

        /// <summary>
        /// Update whatever needs updating. Currently only mouseLocation
        /// </summary>
        public void update()
        {
            //mouse location is given in parent window location, so correct offset
            mouseLocation = getWorldLocation(UserInterface.TVUserInput.MouseState.X - areaOffsetX, 
                                             UserInterface.TVUserInput.MouseState.Y - areaOffsetY);
        }

        /// <summary>
        /// Reset zoom area to show complete track
        /// </summary>
        /// <param name="drawTrackDB">Track database used to determine what the complete track area is</param>
        public void zoomReset(DrawTrackDB drawTrackDB)
        {
            float minX = drawTrackDB.minTileX * 2048 - 1024f;
            float minZ = drawTrackDB.minTileZ * 2048 - 1024f;
            float maxX = drawTrackDB.maxTileX * 2048 + 1024f;
            float maxZ = drawTrackDB.maxTileZ * 2048 + 1024f;
            setDrawArea(minX, maxX, minZ, maxZ);
        }

        /// <summary>
        /// Intitialize the draw area from min and max x and z in world locations
        /// </summary>
        /// <param name="minX">minimal real world X location</param>
        /// <param name="maxX">maximal real world X location</param>
        /// <param name="minY">minimal real world Z location</param>
        /// <param name="maxY">maximal real world Z location</param>
        private void setDrawArea(float minX, float maxX, float minZ, float maxZ)
        { 
            float scaleX = areaW / (maxX - minX);
            float scaleY = areaH / (maxZ - minZ);
            //make square tiles
            scale = Math.Min(scaleX, scaleY);
            metersPerPixel.ApproximateTo(1.0f / scale);
            scale = metersPerPixel.InverseScaleValue;
            fullScale = scale;
            // center the window. Equation we use is areaX = scale * (worldX - offsetX)
            // or offsetX = worldX - areaX/scale. 
            offsetX = (maxX + minX) / 2 - areaW / 2 / scale;
            offsetZ = (maxZ + minZ) / 2 - areaH / 2 / scale;
            postZoomTasks();
        }

        
        /// <summary>
        /// Do a single (small) zoom, seeing more details in the viewer
        /// </summary>
        public void zoomIn()
        {
            zoomAroundMouse(-1);
        }

        /// <summary>
        /// Do a single (small) zoom, seeing less details
        /// </summary>
        public void zoomOut()
        {
            zoomAroundMouse(1);
        }

        /// <summary>
        /// Do a single zoom step around the mouse location
        /// </summary>
        /// <param name="scaleFactor">The zoom-in factor</param>
        public void zoomAroundMouse(int scaleSteps)
        {
            zoomAround(getAreaVector(mouseLocation), scaleSteps);
        }

        /// <summary>
        /// Do a single zoom-in step around the center of the drawing area.
        /// </summary>
        public void zoomInCentered()
        {
            zoomCentered(-1);
        }

        /// <summary>
        /// Do a single zoom-out step around the center of the drawing area.
        /// </summary>
        public void zoomOutCentered()
        {
            zoomCentered(1);
        }

        /// <summary>
        /// Do a single zoom step around the center of the drawing area.
        /// </summary>
        /// <param name="scalefactor">The zoom-in factor</param>
        public void zoomCentered(int scaleSteps)
        {
            zoomAround(new Vector2(areaW / 2, areaH / 2), scaleSteps);
        }

        /// <summary>
        /// Do a single zoom-in step around a certain fixed position.
        /// This is the routine where the actually zooming happens.
        /// </summary>
        /// <param name="fixedAreaLocation">x- and y-coordinates (in pixels) of the fixed position</param>
        /// <param name="scaleFactor">The zoom-in factor</param>
        public void zoomAround(Vector2 fixedAreaLocation, int scaleSteps)
        {
            // prevent too much zooming out
            if ((scaleSteps > 0) && maxZoomOutReached()) return;

            // equation is areaX = scale * (worldX - offsetX)
            // for a fixed point (in area location) we have
            //  fixedX = scale_old * (worldX - offsetX_old) = scale_new * (worldX - offsetX_new)
            //  fixedX/scale_old + offsetX_old = fixedX/scale_new + offsetX_new = worldX
            //  offsetX_new = offsetX_old + fixedX * (scale_new/scale_old - 1) / scale_new
            float scaleFactor = 1.0f/(float)metersPerPixel.AddStep(scaleSteps); // 1.0/xxx because scale is inverse of metersPerPixel
            scale = metersPerPixel.InverseScaleValue;
            offsetX +=          fixedAreaLocation.X  * (scaleFactor - 1) / scale;
            offsetZ += (areaH - fixedAreaLocation.Y) * (scaleFactor - 1) / scale;
            postZoomTasks();
        }


        /// <summary>
        /// Zoom to such a scale that a single tile will fit (in either X or Z-direction)
        /// </summary>
        public void zoomToTile()
        {
            //normal equation: areaX = scale * (worldX - offsetX)
            //zoom to tile: screenW = scale_new * 2048
            double scaleX = areaW / 2048.0;
            double scaleY = areaH / 2048.0;
            double newScale = Math.Min(scaleX, scaleY);
            int stepsNeeded = metersPerPixel.StepsNeededForRatio(scale / newScale );
            zoomAroundMouse(stepsNeeded);
        }

        /// <summary>
        /// Zoom to such a scale that a single tile will fit, center of screen
        /// </summary>
        public void ZoomToTileCentered()
        {
            double scaleX = areaW / 2048.0;
            double scaleY = areaH / 2048.0;
            double newScale = Math.Min(scaleX, scaleY);
            zoomCentered(metersPerPixel.StepsNeededForRatio(scale / newScale ));
        }

        /// <summary>
        /// Determines whether the maximum allowed zoom is reached
        /// </summary>
        /// <returns>true when we should no longer zoom-out</returns>
        public bool maxZoomOutReached()
        {
            return (scale < fullScale/2);
        }

        private void postZoomTasks()
        {
            if (drawScaleRuler != null) drawScaleRuler.SetCurrentRuler(scale);
        }

        /// <summary>
        /// Adjust the scale etc to follow another drawArea, showing either the full scale or at max a scale
        /// that is maxScaleRatio difference
        /// </summary>
        /// <param name="drawarea">The other drawArea that needs to be followed</param>
        /// <param name="maxScaleDifference">Maximal difference in scale</param>
        public void Follow(DrawArea otherArea, float maxScaleRatio)
        {
            double otherScale = otherArea.scale;
            double otherScaleCorrected = otherScale * this.areaW / otherArea.areaW; // Correct for different screen size
            scale = Math.Max(fullScale, otherScaleCorrected/maxScaleRatio);
            // center on the same center as otherArea
            // other.screenW/2 = otherScale * (WorldX - other.offsetX)
            // this.screenW/2  = this.Scale * (WorldX - this.offsetX);
            // so WorldX = other.offsetX + other.screenW/2 / otherScale
            this.offsetX = otherArea.offsetX + otherArea.areaW / 2 / otherArea.scale - this.areaW / 2 / this.scale;
            this.offsetZ = otherArea.offsetZ + otherArea.areaH / 2 / otherArea.scale - this.areaH / 2 / this.scale;
            mouseLocation = otherArea.mouseLocation;
            postZoomTasks();
        }

        /// <summary>
        /// Shift the view window to the left
        /// </summary>
        public void ShiftLeft()
        {
            offsetX -= shiftRatioDefault * areaW / scale / 2;
        }

        /// <summary>
        /// Shift the view window to the right
        /// </summary>
        public void ShiftRight()
        {
            offsetX += shiftRatioDefault * areaW / scale / 2;
        }

        /// <summary>
        /// Shift the view window up
        /// </summary>
        public void ShiftUp()
        {
            offsetZ += shiftRatioDefault * areaH / scale / 2;
        }

        /// <summary>
        /// Shift the view window down
        /// </summary>
        public void ShiftDown()
        {
            offsetZ -= shiftRatioDefault * areaH / scale / 2;
        }

        /// <summary>
        /// Shift the view window with a number of pixels (e.g. to follow a mouse)
        /// </summary>
        /// <param name="pixelsX">Number of pixels to shift in X direction</param>
        /// <param name="pixelsY">Number of pixels to shift in Y direction</param>
        public void ShiftArea(int pixelsX, int pixelsY)
        {
            offsetX -= pixelsX / scale;
            offsetZ += pixelsY / scale;
        }

        public void ShiftToLocation(WorldLocation location)
        {
            // Basic equation areaX = scale * (worldX - offsetX)
            // We want middle of screen to shift to new worldX, so areaW/2 = scale * (worldX - offsetX)
            // Similarly
            double worldX = location.TileX * 2048 + location.Location.X;
            double worldZ = location.TileZ * 2048 + location.Location.Z;
            offsetX = worldX - areaW / (2 * scale);
            offsetZ = worldZ - areaH / (2 * scale);
        }


        /// <summary>
        /// Translate world sizes to screen pixels (rounded up to make sure something is drawn).
        /// Note, size in area and size in window are the same.
        /// </summary>
        /// <param name="worldSize">size in the real world in meters</param>
        /// <returns>size in window pixels</returns>
        private float getWindowSize(float worldSize)
        {
            return (float) Math.Ceiling ( scale * worldSize);
        }

        /// <summary>
        /// Translate a WorldLocation to an area location
        /// </summary>
        /// <param name="location">location in World coordinates (including tiles)</param>
        /// <returns>location on the drawing area in a 2d vector (in pixels)</returns>
        private Vector2 getAreaVector(WorldLocation location)
        {
            double x = location.TileX * 2048 + location.Location.X;
            double y = location.TileZ * 2048 + location.Location.Z;
            return new Vector2((float)(        scale * ( x - offsetX)),
                               (float)(areaH - scale * ( y - offsetZ)));
        }

        /// <summary>
        /// Translate a WorldLocation to parent window coordinates
        /// </summary>
        /// <param name="location">location in World coordinates (including tiles)</param>
        /// <returns>location on the parent window in a 2d vector (in pixels)</returns>
        private Vector2 getWindowVector(WorldLocation location)
        {
            Vector2 windowVector = getAreaVector(location);
            windowVector.X += areaOffsetX;
            windowVector.Y += areaOffsetY;
            return windowVector; 
        }

        /// <summary>
        /// Translate a area location into a parent window location
        /// </summary>
        /// <param name="areaX">X-location (in pixels relative to drawing area)</param>
        /// <param name="areaY">Y-location (in pixels relative to drawing area)</param>
        /// <returns>location on the parent window in a 2d vector</returns>
        private Vector2 getWindowVector(int areaX, int areaY)
        {
            return new Vector2(areaX + areaOffsetX, areaY + areaOffsetY);
        }

        /// <summary>
        /// Calculate back the World location corresponding to given area coordinate
        /// </summary>
        /// <param name="areaX">X-coordinate on the area (in pixels)</param>
        /// <param name="areaY">Y-coordinate on the area (in pixels)</param>
        /// <returns>Corresponding world location</returns>
        private WorldLocation getWorldLocation(int areaX, int areaY)
        {
            double x = (offsetX + (        areaX) / scale);
            double z = (offsetZ + (areaH - areaY) / scale);
            //WorldLocation location = new WorldLocation(0, 0, x, 0, z);
            //we now do pre-normalization. This normalization is more efficient than Coordinates.normalization
            int tileX = (int) x / 2048;
            x -= (tileX * 2048);
            int tileZ = (int)z / 2048;
            z -= tileZ * 2048;
            WorldLocation location = new WorldLocation(tileX, tileZ, (float)x, 0, (float)z);
            location.Normalize();
            return location;
        }

        /// <summary>
        /// Check whether a given bool is outside the drawing area (or not).
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>Boolean describing describing whether the point is out</returns>
        public bool OutOfArea(WorldLocation point)
        {
            Vector2 areaVector = getAreaVector(point);
            float leeway = (strictChecking) ? 0 : areaW;
            if (areaVector.X < -leeway) return true;
            if (areaVector.Y < -leeway) return true;
            if (areaVector.X > areaW + leeway) return true;
            if (areaVector.Y > areaH + leeway) return true;
            return false;
        }

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
            BasicShapes.DrawLine(getWindowSize(width), color, getWindowVector(point1), getWindowVector(point2));
        }
        public void DrawLineAlways(float width, Color color, WorldLocation point1, WorldLocation point2)
        {
            BasicShapes.DrawLine(getWindowSize(width), color, getWindowVector(point1), getWindowVector(point2));
        }

        /// <summary>
        /// Basic method to draw a line starting at a point between two points. 
        /// Parameters are in world coordinates and sizes.
        /// </summary>
        /// <param name="width"> Width of the line to draw in meters </param>
        /// <param name="color"> Color of the line</param>
        /// <param name="point"> WorldLocation of the first point of the line (for zero offset)</param>
        /// <param name="length"> length of the line to draw in meters (also when shifted by offset)</param>
        /// <param name="angle"> Angle (in degrees east of North)</param>
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
            BasicShapes.DrawLine(getWindowSize(width), color, getWindowVector(beginPoint), 
                getWindowSize(length), angle-MathHelper.Pi/2);
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
                beginPoint.Location.X += lengthOffset * (float)Math.Sin(angle + arcRadOffset/2);
                beginPoint.Location.Z += lengthOffset * (float)Math.Cos(angle + arcRadOffset/2);
            }
            WorldLocation endPoint = new WorldLocation(beginPoint); //approximate location of end-point
            float arcRad = arcDegrees * MathHelper.Pi / 180;
            float length = radius * arcRad;
            endPoint.Location.X += length * (float)Math.Sin(angle + arcRad/2);
            endPoint.Location.Z += length * (float)Math.Cos(angle + arcRad/2);
            if (OutOfArea(beginPoint) && OutOfArea(endPoint)) return;
            // for the 90 degree offset, see DrawLine
            BasicShapes.DrawArc(getWindowSize(width), color, getWindowVector(point),
                getWindowSize(radius), angle - MathHelper.Pi / 2, arcDegrees, arcDegreesOffset);
        }

    


        /// <summary>
        /// Draw a vertical line trough the given world line, all the way from bottom to top. 1 pixel wide.
        /// </summary>
        /// <param name="color">Color of the line</param>
        /// <param name="point">Worldlocation through which to draw the line</param>
        private void DrawLineVertical(Color color, WorldLocation point)
        {
            Vector2 middle = getWindowVector(point);
            Vector2 top = getWindowVector(0, 0);
            Vector2 bot = getWindowVector(0, areaH);
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
            Vector2 middle = getWindowVector(point);
            Vector2 left = getWindowVector(0,0);
            Vector2 right = getWindowVector(areaW, 0);
            right.Y = middle.Y;
            left.Y  = middle.Y;
            BasicShapes.DrawLine(1, color, left, right);
        }

        /// <summary>
        /// Draw the grid consisting of the boundaries of the WorldTiles within the drawing area
        /// </summary>
        public void DrawTileGrid()
        {
            WorldLocation locationUpperLeft = getWorldLocation(0, 0);
            WorldLocation locationLowerRight = getWorldLocation(areaW, areaH);
            // draw tile Grid boundaries. Note that coordinates within tile are from -1024 to 1024
            for (int tileX = locationUpperLeft.TileX; tileX <= locationLowerRight.TileX + 1; tileX++)
            {
                DrawLineVertical(Color.Gray, new WorldLocation(tileX, 0, -1024f, 0f, 0f));
            }
            for (int tileZ = locationLowerRight.TileZ; tileZ <= locationUpperLeft.TileZ + 1; tileZ++)
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
            BasicShapes.DrawLine(areaW, color, getWindowVector(areaW/2, 0), getWindowVector(areaW/2, areaH));
        }

        /// <summary>
        /// Draw a border around the area
        /// </summary>
        /// <param name="color">The color of the border line</param>
        public void DrawBorder(Color color)
        {
            // We do not use DrawBorder below to prevent a line not being drawn because of OutOfArea
            BasicShapes.DrawLine(1, color, getWindowVector(0    , 0    ), getWindowVector(0    , areaH));
            BasicShapes.DrawLine(1, color, getWindowVector(areaW, 0    ), getWindowVector(areaW, areaH));
            BasicShapes.DrawLine(1, color, getWindowVector(0    , 0    ), getWindowVector(areaW, 0    ));
            BasicShapes.DrawLine(1, color, getWindowVector(0    , areaH), getWindowVector(areaW, areaH));
        }

        /// <summary>
        /// Draw the border (in world coordinates) of another area (e.g. showing a zoom window).
        /// </summary>
        /// <param name="color">The color of the border line</param>
        /// <param name="drawArea">The other area of which the border needs to be drawn</param>
        public void DrawBorder(Color color, DrawArea OtherDrawArea)
        {
            WorldLocation locationUpperLeft  = OtherDrawArea.getWorldLocation(0                  , 0);
            WorldLocation locationUpperRight = OtherDrawArea.getWorldLocation(OtherDrawArea.areaW, 0);
            WorldLocation locationLowerLeft  = OtherDrawArea.getWorldLocation(0                  , OtherDrawArea.areaH);
            WorldLocation locationLowerRight = OtherDrawArea.getWorldLocation(OtherDrawArea.areaW, OtherDrawArea.areaH);

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
        public void DrawString(WorldLocation location, string message)
        {
            if (OutOfArea(location)) return;
            Vector2 textOffset = new Vector2(3, 3); // We offset the top-left corner to make sure the text is not on the marker.
            BasicShapes.DrawString(getWindowVector(location)+textOffset, DrawColors.colorsNormal["text"], message); 
        }

        /// <summary>
        /// Draw a texture, determined by its name.
        /// </summary>
        /// <param name="location">Location where to draw the texture</param>
        /// <param name="textureName">Name identifying the texture</param>
        /// <param name="angle">Rotation angle for the texture</param>
        /// <param name="size">Size of the texture in world-meters</param>
        /// <param name="minPixelSize">Minimum size in pixels, to make sure you always see something</param>
        public void DrawTexture(WorldLocation location, string textureName, float angle, float size, int minPixelSize)
        {
            if (OutOfArea(location)) return;
            float pixelSize = (float)Math.Max(getWindowSize(size), minPixelSize);
            BasicShapes.DrawTexture(getWindowVector(location), textureName, angle, pixelSize, Color.White);
        }

        /// <summary>
        /// Draw a simple texture, like ring, disc, determined by its name. In contrast to regular textures
        /// here we support color as well, because that is not predefined
        /// </summary>
        
        /// <param name="location">Location to draw (the center) of the texture</param>
        /// <param name="size">size in meters of the texture</param>
        /// <param name="textureName">Name of the simple texture, like disc, ring, </param>
        /// <param name="minPixelSize">Minimum size in pixels, to make sure you always see something</param>
        /// <param name="color">Color you want the simple texture to have</param>
        public void DrawSimpleTexture(WorldLocation location, string textureName, float size, int minPixelSize, Color color)
        {
            if (OutOfArea(location)) return;
            float pixelSize = (float)Math.Max(getWindowSize(size), minPixelSize);
            BasicShapes.DrawTexture(getWindowVector(location), textureName, 0, pixelSize, color);
        }

    }

    class DiscreteScale
    {
        private static int[] discreteSteps = { 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 27, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 90 };
        private int stepIndex = 0;
        private int powerOfTen = 0;

        public double ScaleValue { get { return discreteSteps[stepIndex] * Math.Pow(10.0, (double) powerOfTen); } }
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
            }
            return ScaleValue / previousValue;
        }

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

        public int StepsNeededForRatio(double ratio)
        {
            DiscreteScale newScale = new DiscreteScale();
            newScale.ApproximateTo(ratio * ScaleValue);
            int neededSteps = newScale.stepIndex - this.stepIndex;
            neededSteps += discreteSteps.Count() * (newScale.powerOfTen - this.powerOfTen);
            return neededSteps;
        }
    }
}
