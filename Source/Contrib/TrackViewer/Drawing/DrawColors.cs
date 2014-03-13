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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// This class contains the definition of all colors used during drawing.
    /// By having that in one class it is much easier to change colors in case that is needed.
    /// Different colorScheme's are supported. This means that drawing, for instance, a track
    /// can be done from different parts of the program using a different colorscheme, such that the drawing gets the color
    /// belonging to that colorscheme. This allows reusing the drawing routine and still get different colors.
    /// The colorschemes themselves are also defined here.
    /// 
    /// </summary>
    /// <remarks> Currently both colorschemes and colors are identified by a  string. Possibly it is better to use enumerations.
    /// </remarks>
    static class DrawColors
    {
        public static ColorScheme colorsNormal    = new ColorScheme();
        public static ColorScheme colorsHighlight = new ColorScheme("Highlight");
        public static ColorScheme colorsHotlight  = new ColorScheme("Hotlight");  // even more highlighting
        public static ColorScheme colorsRoads = new ColorScheme();
        public static ColorScheme colorsRoadsHighlight = new ColorScheme("Highlight");
        public static ColorScheme colorsRoadsHotlight = new ColorScheme("Hotlight");
        public static ColorScheme colorsPathMain = new ColorScheme();
        public static ColorScheme colorsPathSiding = new ColorScheme();

        static readonly Dictionary<string, Color> namedColors =
        typeof(Color).GetProperties()
                     .Where(prop => prop.PropertyType == typeof(Color))
                     .ToDictionary(prop => prop.Name,
                                   prop => (Color)prop.GetValue(null, null));


        /// <summary>
        /// Do the initialization of the settings (set the defaults)
        /// </summary>
        public static void Initialize(ORTS.TrackViewer.UserInterface.MenuControl menuControl)
        {

            colorsNormal["clearwindowinset"] = Color.LightBlue;
            colorsNormal["pathMain"] = Color.Yellow;
            colorsNormal["pathSiding"] = Color.Orange;
            colorsNormal["pathBroken"] = Color.Salmon;
            colorsNormal["activeNode"] = Color.Purple;
            colorsNormal["nodeCandidate"] = Color.Blue;
            colorsNormal["brokenNode"] = Color.Red;
            colorsNormal["siding"] = Color.Sienna;
            colorsNormal["platform"] = Color.Aqua;
            colorsNormal["text"] = Color.Black;
            colorsNormal["crossing"] = Color.Gray;
            colorsNormal["road crossing"] = Color.DarkGray;
            colorsNormal["speedpost"] = Color.Purple;

            foreach (string colorName in colorsNormal.Keys) {
                colorsHighlight[colorName] = Highlighted(colorsNormal[colorName], 30);
                colorsHotlight[colorName] = Highlighted(colorsHighlight[colorName], 30);
                colorsRoads[colorName] = colorsNormal[colorName];
                colorsPathMain[colorName] = colorsNormal[colorName];
                colorsPathSiding[colorName] = colorsNormal[colorName];
            }

            colorsPathMain["trackStraight"] = Color.Yellow;
            colorsPathMain["trackCurved"] = Color.Yellow;
            colorsPathSiding["trackStraight"] = Color.Orange;
            colorsPathSiding["trackCurved"] = Color.Orange;

         }

        /// <summary>
        /// Make a highlight variant of the color (making it more white).
        /// </summary>
        /// <param name="color">reference color</param>
        /// <param name="offset">value that is added to each color channel</param>
        /// <returns>Scaled color</returns>
        public static Color Highlighted(Color color, byte offset)
        {
            Color newColor = new Color();
            newColor.A = color.A;
            byte effectiveOffset = (byte)( (color.A > 128) ? offset : 0);
            newColor.B = (byte)Math.Min(color.B + effectiveOffset, 255);
            newColor.R = (byte)Math.Min(color.R + effectiveOffset, 255);
            newColor.G = (byte)Math.Min(color.G + effectiveOffset, 255);
            return newColor;
            //return new Color(color.ToVector4() * scale);
        }

        /// <summary>
        /// Set the track coloring depening on user choise
        /// </summary>
        /// <param name="doColoring">Boolean describing whether tracks will be colored or not (i.e. using a flat color)</param>
        /// <param name="doTiles">Boolean describing whether tiles will be shown or not</param>
        public static void setTrackColors(bool doColoring, bool doTiles)
        {
            if (doTiles)
            {
                colorsNormal["clearwindow"] = Color.White;
                colorsNormal["tile"] = colorsNormal["background"];
            }
            else
            {
                colorsNormal["clearwindow"] = colorsNormal["background"];
            }
            if (doColoring)
            {
                setTrackColorsColoured();
            }
            else
            {
                setTrackColorsFlat();
            }
        }


        // Default string for menu
        static readonly string defaultColorName = "PaleGreen";
        static readonly string defaultColorString = defaultColorName + " (default)";
        /// <summary>
        /// Set the SetBackGroundColor using string as input. Return whether this succeeded or not
        /// </summary>
        /// <param name="colorName"></param>
        public static bool SetBackGroundColor(string colorName)
        {
            if (colorName == defaultColorString)
            {
                colorName = defaultColorName;
            }
            if (namedColors.ContainsKey(colorName))
            {
                colorsNormal["background"] = namedColors[colorName];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Return a list of colornames including a default. Also set the initial background color
        /// </summary>
        /// <returns>List of colornames for the menu</returns>
        public static List<string> GetColorNames (string preferenceBackgroundColor)
        {
            if (preferenceBackgroundColor == defaultColorString)
            {
                preferenceBackgroundColor = defaultColorName;
            }
            colorsNormal["background"] = namedColors[preferenceBackgroundColor];
            List<string> colorNames = namedColors.Keys.ToList();
            colorNames.Insert(0, defaultColorString);
            return colorNames;
        }

        /// <summary>
        /// Set the colour schemes for coloured tracks
        /// </summary>
        private static void setTrackColorsColoured() {
            colorsNormal["trackStraight"] = Color.Black;
            colorsNormal["trackCurved"] = Color.Green;
            colorsNormal["trackSwitch"] = Color.Purple;
            colorsNormal["junction"] = Color.Blue;
            colorsNormal["endnode"] = Color.LimeGreen;

            colorsHighlight["trackStraight"] = Color.Red;
            colorsHighlight["trackCurved"] = Color.Tomato;
            colorsHighlight["trackSwitch"] = Color.Pink;
            colorsHighlight["junction"] = Color.Azure;
            colorsHighlight["endnode"] = Color.Lime;

            colorsHotlight["trackStraight"] = Color.LightGoldenrodYellow;
            colorsHotlight["trackCurved"] = Color.LightGoldenrodYellow;
            colorsHotlight["trackSwitch"] = Color.Pink;
            colorsHotlight["junction"] = Color.Azure;
            colorsHotlight["endnode"] = Color.Lime;

            colorsRoads["trackStraight"] = Color.Gray;
            colorsRoads["trackCurved"] = Color.DarkOliveGreen;

            colorsRoadsHighlight["trackStraight"] = Highlighted(colorsRoads["trackStraight"], 40);
            colorsRoadsHighlight["trackCurved"] = Highlighted(colorsRoads["trackCurved"], 40);

            colorsRoadsHotlight["trackStraight"] = Highlighted(colorsRoads["trackStraight"], 80);
            colorsRoadsHotlight["trackCurved"] = Highlighted(colorsRoads["trackCurved"], 80);

        }

        /// <summary>
        /// Set the colour schemes for non-coloured tracks
        /// </summary>
        private static void setTrackColorsFlat() {
            colorsNormal["trackStraight"] = Color.Black;
            colorsNormal["trackCurved"] = Color.Black;
            colorsNormal["trackSwitch"] = Color.Black;
            colorsNormal["junction"] = Color.Blue;
            colorsNormal["endnode"] = Color.LimeGreen;

            colorsHighlight["trackStraight"] = Color.Tomato;
            colorsHighlight["trackCurved"] = Color.Tomato;
            colorsHighlight["trackSwitch"] = Color.Tomato;
            colorsHighlight["junction"] = Color.LightBlue;
            colorsHighlight["endnode"] = Color.Lime;

            colorsHotlight["trackStraight"] = Color.Red;
            colorsHotlight["trackCurved"] = Color.Red;
            colorsHotlight["trackSwitch"] = Color.Red;
            colorsHotlight["junction"] = Color.LightBlue;
            colorsHotlight["endnode"] = Color.Lime;

            colorsRoads["trackStraight"] = Color.DarkGray;
            colorsRoads["trackCurved"] = Color.DarkGray;

            colorsRoadsHighlight["trackStraight"] = Highlighted(colorsRoads["trackStraight"], 20);
            colorsRoadsHighlight["trackCurved"] = Highlighted(colorsRoads["trackCurved"], 20);

            colorsRoadsHotlight["trackStraight"] = Highlighted(colorsRoads["trackStraight"], 40);
            colorsRoadsHotlight["trackCurved"] = Highlighted(colorsRoads["trackCurved"], 40);

        }

    }

    /// <summary>
    /// Class to store colors used for drawing tracks etc. Exists mainly to facilitate highlight colors
    /// </summary>
    class ColorScheme : Dictionary<string,Color>
    {
        public string nameExtension;

        public ColorScheme()
        {
        }

        public ColorScheme(string nameExtension)
        {
            this.nameExtension = nameExtension;
        }
        
    }
}
