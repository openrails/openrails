// COPYRIGHT 2023 by the Open Rails project.
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
//

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orts.Viewer3D.Popups
{
    public class OutOfFocusWindow : Window
    {
        private readonly Texture2D Line;
        private readonly int Thickness = 3;
        private readonly Color Color = Color.Red;

        private readonly int Width;
        private readonly int Height;

        public OutOfFocusWindow(WindowManager owner) : base(owner)
        {
            Line = new Texture2D(owner.Viewer.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Line.SetData(new[] { Color });

            Width = owner.Viewer.GraphicsDevice.Viewport.Width;
            Height = owner.Viewer.GraphicsDevice.Viewport.Height;
        }

        public override void Draw(SpriteBatch SpriteBatch)
        {
            // top
            DrawLine(SpriteBatch, 0, 0, Width, Thickness, 0);

            // bottom
            DrawLine(SpriteBatch, 0, Height - Thickness, Width, Thickness, 0);

            // left
            DrawLine(SpriteBatch, Thickness, Thickness, Height, Thickness, 90);

            // right
            DrawLine(SpriteBatch, Width, Thickness, Height, Thickness, 90);
        }

        private void DrawLine(SpriteBatch SpriteBatch, int X, int Y, int width, int height, int degrees)
        {
            SpriteBatch.Draw(
                Line,
                new Rectangle(X, Y, width, height),
                null,
                Color,
                (float)ConvertToRadiansFromDegrees(degrees),
                new Vector2(0, 0),
                SpriteEffects.None, 0);
         }

        private float ConvertToRadiansFromDegrees(int angle)
        {
            return (float)((System.Math.PI / 180) * angle);
        }
    }
}
