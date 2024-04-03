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
using Microsoft.Xna.Framework;
using ORTS.Common;
using Orts.Common;

namespace ORTS.Orge.Drawing
{
    /// <summary>
    /// Class to enable the drawing of longitude and latitude in real world coordinates
    /// </summary>
    public class DrawLongitudeLatitude
    {
        WorldLatLon worldLoc; // private field to prevent having the create the object over and over again.
        Vector2 lowerLeft;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xLowerLeft">Lower left x-value where to print the location in pixels</param>
        /// <param name="yLowerLeft">Lower left y-value where to print the location in pixels</param>
        public DrawLongitudeLatitude(int xLowerLeft, int yLowerLeft)
        {
            worldLoc = new WorldLatLon();
            lowerLeft = new Vector2(xLowerLeft, yLowerLeft);

        }

        /// <summary>
        /// Draw (print) the values of longitude and latitude
        /// </summary>
        /// <param name="mstsLocation">MSTS Location which to translate to real world coordinates</param>
        public void Draw(WorldLocation mstsLocation)
        {
            if (!Properties.Settings.Default.showLonLat) return;
            
            double latitude = 1f;
            double longitude = 1f;
            worldLoc.ConvertWTC(mstsLocation.TileX, mstsLocation.TileZ, mstsLocation.Location, ref latitude, ref longitude);
            string latitudeDegrees = MathHelper.ToDegrees((float)latitude).ToString("F5", System.Globalization.CultureInfo.CurrentCulture);
            string longitudeDegrees = MathHelper.ToDegrees((float)longitude).ToString("F5", System.Globalization.CultureInfo.CurrentCulture);
            string locationText = String.Format(System.Globalization.CultureInfo.CurrentCulture, 
                "Lon = {0}; Lat = {1}", longitudeDegrees, latitudeDegrees);
            BasicShapes.DrawString(lowerLeft, DrawColors.colorsNormal.Text, locationText);           
        }
    }
}
