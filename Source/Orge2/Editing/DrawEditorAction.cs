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

using Microsoft.Xna.Framework;
using ORTS.Orge.Drawing;

namespace ORTS.Orge.Editing
{
    /// <summary>
    /// Small class only to draw the current possible editor action on the screen
    /// </summary>
    public class DrawEditorAction
    {
        Vector2 lowerLeft;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xLowerLeft">Location of the text on screen in the x-direction</param>
        /// <param name="yLowerLeft">Location of the text on screen in the y-direction</param>
        public DrawEditorAction(int xLowerLeft, int yLowerLeft)
        {
            lowerLeft = new Vector2(xLowerLeft, yLowerLeft);
        }

    
        /// <summary>
        /// Draw (print) the values of longitude and latitude
        /// </summary>
        /// <param name="editor">The patheditor for which we need to draw the status</param>
        public void Draw(PathEditor editor)
        {
            if (editor == null) { return; }
            if (!Properties.Settings.Default.showEditorAction) { return;}
            BasicShapes.DrawString(lowerLeft, DrawColors.colorsNormal.Text, editor.CurrentActionDescription);
        }
    }

}
