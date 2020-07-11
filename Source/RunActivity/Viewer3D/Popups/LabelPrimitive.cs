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

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;

namespace Orts.Viewer3D.Popups
{
    public class LabelPrimitive : RenderPrimitive
    {
        readonly Label3DMaterial Material;

        public WorldPosition Position;
        public string Text;
        public Color Color;
        public Color Outline;

        readonly Viewer Viewer;
        readonly float OffsetY;

        public LabelPrimitive(Label3DMaterial material, Color color, Color outline, float offsetY)
        {
            Material = material;
            Viewer = material.Viewer;
            Color = color;
            Outline = outline;
            OffsetY = offsetY;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            var lineLocation3D = Position.XNAMatrix.Translation;
            lineLocation3D.X += (Position.TileX - Viewer.Camera.TileX) * 2048;
            lineLocation3D.Y += OffsetY;
            lineLocation3D.Z += (Viewer.Camera.TileZ - Position.TileZ) * 2048;

            var lineLocation2DStart = Viewer.GraphicsDevice.Viewport.Project(lineLocation3D, Viewer.Camera.XnaProjection, Viewer.Camera.XnaView, Matrix.Identity);
            if (lineLocation2DStart.Z > 1 || lineLocation2DStart.Z < 0)
                return; // Out of range or behind the camera

            lineLocation3D.Y += 10;
            var lineLocation2DEndY = Viewer.GraphicsDevice.Viewport.Project(lineLocation3D, Viewer.Camera.XnaProjection, Viewer.Camera.XnaView, Matrix.Identity).Y;

            var labelLocation2D = Material.GetTextLocation((int)lineLocation2DStart.X, (int)lineLocation2DEndY - Material.Font.Height, Text);
            lineLocation2DEndY = labelLocation2D.Y + Material.Font.Height;

            Material.Font.Draw(Material.SpriteBatch, labelLocation2D, Text, Color, Outline);
            Material.SpriteBatch.Draw(Material.Texture, new Vector2(lineLocation2DStart.X - 1, lineLocation2DEndY), null, Outline, 0, Vector2.Zero, new Vector2(4, lineLocation2DStart.Y - lineLocation2DEndY), SpriteEffects.None, lineLocation2DStart.Z);
            Material.SpriteBatch.Draw(Material.Texture, new Vector2(lineLocation2DStart.X, lineLocation2DEndY), null, Color, 0, Vector2.Zero, new Vector2(2, lineLocation2DStart.Y - lineLocation2DEndY), SpriteEffects.None, lineLocation2DStart.Z);
        }
    }
}
