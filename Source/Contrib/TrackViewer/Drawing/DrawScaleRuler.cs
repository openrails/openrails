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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// This class draws the scale of the current viewing area (showing how long a certain distance is in world-coordinates)
    /// The scale actually draws a few straight lines, as well as the text showing the size of the ruler as well as the scale.
    /// Nice round numbers for the scales are supported, both in meters and in mile/yards.
    /// </summary>
    public class DrawScaleRuler
    {
        struct RulerDatum
        {
            public float value;
            public string text;
            public int subMarkers;
            public RulerDatum(int subMarkers, float value, string text)
            {
                this.value = value;
                this.text = text;
                this.subMarkers = subMarkers;
            }
        }

        private List<RulerDatum> rulerDataMeters = new List<RulerDatum>();
        private List<RulerDatum> rulerDataMiles = new List<RulerDatum>();
        private RulerDatum currentRuler;

        private int maxPixelWidth = 200; // we do not want a wider scale than this.
        private Vector2 lowerLeftPoint;  // lower left point we can use for drawing
        private int halfFontHeight;      // Height of the font being used
        private int fullPixelWidth;      // width of the scale
        private double pixelsPerMeter;   // store it in case we change meters to miles v.v.
        private bool useMilesNotMeters;  // we need to store this to be able to check whether it changed

        /// <summary>
        /// Constructor.
        /// </summary>
        public DrawScaleRuler()
        {
            // here we define, a bit manually, which scales are supported.
            rulerDataMeters.Add(new RulerDatum(5, 100000, " 100km"));
            rulerDataMeters.Add(new RulerDatum(5, 50000, " 50km"));
            rulerDataMeters.Add(new RulerDatum(4, 20000, " 20km")); 
            rulerDataMeters.Add(new RulerDatum(5, 10000, " 10km"));
            rulerDataMeters.Add(new RulerDatum(5, 5000, " 5km"));
            rulerDataMeters.Add(new RulerDatum(4, 2000, " 2km"));
            rulerDataMeters.Add(new RulerDatum(5, 1000, " 1km"));
            rulerDataMeters.Add(new RulerDatum(5, 500, " 500m"));
            rulerDataMeters.Add(new RulerDatum(4, 200, " 200m"));
            rulerDataMeters.Add(new RulerDatum(5, 100, " 100m"));
            rulerDataMeters.Add(new RulerDatum(5, 50, " 50m"));
            rulerDataMeters.Add(new RulerDatum(4, 20, " 20m"));
            rulerDataMeters.Add(new RulerDatum(5, 10, " 10m"));
            rulerDataMeters.Add(new RulerDatum(5, 5, " 5m"));
            rulerDataMeters.Add(new RulerDatum(4, 2, " 2m"));
            rulerDataMeters.Add(new RulerDatum(5, 1, " 1m"));

            rulerDataMiles.Add(new RulerDatum(5, 80450, " 50mi"));
            rulerDataMiles.Add(new RulerDatum(4, 32180, " 20mi"));
            rulerDataMiles.Add(new RulerDatum(5, 16090, " 10mi"));
            rulerDataMiles.Add(new RulerDatum(5, 8045, " 5mi"));
            rulerDataMiles.Add(new RulerDatum(4, 3218, " 2mi"));
            rulerDataMiles.Add(new RulerDatum(5, 1609, " 1mi"));
            rulerDataMiles.Add(new RulerDatum(4, 731, " 800yd"));
            rulerDataMiles.Add(new RulerDatum(4, 366, " 400yd"));
            rulerDataMiles.Add(new RulerDatum(4, 183, " 200yd"));
            rulerDataMiles.Add(new RulerDatum(5, 91.4f, " 100yd"));
            rulerDataMiles.Add(new RulerDatum(5, 45.7f, " 50yd"));
            rulerDataMiles.Add(new RulerDatum(4, 18.2f, " 20yd"));
            rulerDataMiles.Add(new RulerDatum(5, 9.14f, " 10yd"));
            rulerDataMiles.Add(new RulerDatum(5, 4.57f, " 5yd"));
            rulerDataMiles.Add(new RulerDatum(4, 1.82f, " 2yd"));
            rulerDataMiles.Add(new RulerDatum(5, 0.91f, " 1yd"));
            
        }

        /// <summary>
        /// Simply give the lower-left point we can use for drawing the ruler, as well as the size of the font
        /// </summary>
        /// <param name="xLowerLeft">x-value of the point</param>
        /// <param name="yLowerLeft">y-value of the point</param>
        /// <param name="fontHeight">Height of the current font in pixels</param>
        public void SetLocationAndSize(int xLowerLeft, int yLowerLeft, int fontHeight)
        {
            lowerLeftPoint = new Vector2(xLowerLeft, yLowerLeft);
            halfFontHeight = (int)(fontHeight/2);
            maxPixelWidth = 11 * fontHeight;
        }

        /// <summary>
        /// Figure out, from the scale, which maximum scale we will use and what the corresponding width is on screen
        /// </summary>
        /// <param name="pixelsPerMeter"> ratio between real life and screen pixels</param>
        public void SetCurrentRuler(double pixelsPerMeter)
        {
            this.pixelsPerMeter = pixelsPerMeter;
            this.useMilesNotMeters = Properties.Settings.Default.useMilesNotMeters;

            List<RulerDatum> rulerData = (Properties.Settings.Default.useMilesNotMeters) ? rulerDataMiles : rulerDataMeters;

              
            // to make sure we have something for all zoom levels 
            currentRuler = rulerData.Last();
            
            foreach (RulerDatum rulerDatum in rulerData)
            {
                int pixelWidth = (int) Math.Round(pixelsPerMeter * rulerDatum.value);
                if ( pixelWidth < maxPixelWidth ) {
                    currentRuler = rulerDatum;
                    break;
                }
            }

            fullPixelWidth = (int) Math.Round(pixelsPerMeter * currentRuler.value);
        }

        /// <summary>
        /// Draw the ruler on the screen
        /// </summary>
        public void Draw()
        {
            if (!Properties.Settings.Default.showScaleRuler) return;
            if (Properties.Settings.Default.useMilesNotMeters != useMilesNotMeters)
            {
                SetCurrentRuler(pixelsPerMeter); // Only do this when needed
            }

            string scaleText = " (" + (1.0f / pixelsPerMeter).ToString(System.Globalization.CultureInfo.CurrentCulture) + "m/pixel)";

            Vector2 lowerRightPoint = new Vector2(lowerLeftPoint.X + fullPixelWidth, lowerLeftPoint.Y);
            Vector2 bigMarker = new Vector2(0, -halfFontHeight);
            Vector2 smallMarker = new Vector2(0, -(int)(halfFontHeight/2));
            Color color = DrawColors.colorsNormal.Text;

            BasicShapes.DrawLine(1, color, lowerLeftPoint, lowerRightPoint);
            BasicShapes.DrawLine(1, color, lowerLeftPoint, lowerLeftPoint + bigMarker);
            BasicShapes.DrawLine(1, color, lowerRightPoint, lowerRightPoint + bigMarker);
            BasicShapes.DrawString(lowerRightPoint + bigMarker, color, currentRuler.text + scaleText);
            for (int i = 1; i < currentRuler.subMarkers; i++)
            {
                Vector2 smallMarkerPoint = new Vector2(lowerLeftPoint.X + fullPixelWidth * i / currentRuler.subMarkers, lowerLeftPoint.Y);
                BasicShapes.DrawLine(1, color, smallMarkerPoint, smallMarkerPoint + smallMarker);
            }

        }
    }
}
