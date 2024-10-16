// COPYRIGHT 2024 by the Open Rails project.
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.RollingStock;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;

namespace Orts.Viewer3D.Popups
{
    public class ControlRectangle : Window
    {
        private readonly Texture2D Line;
        private int Thickness = 1;
        private readonly Color Color = Color.Yellow;
        private readonly Viewer Viewer;
        private bool CabViewFront;
        private bool IsOverRectangle = false;
        private class ListRect
        {
            public bool Front;
            public Rectangle CabRectangle;
            public string Name;
        }
        private List<ListRect> ListRectangles = new List<ListRect>();
        private ListRect ListRects;

        public ControlRectangle(WindowManager owner, Viewer viewer) : base(owner)
        {
            Line = new Texture2D(Owner.Viewer.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Line.SetData(new[] { Color });
            Viewer = viewer;
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Viewer.Camera is CabCamera && (Viewer.PlayerLocomotiveViewer as MSTSLocomotiveViewer)._hasCabRenderer)
            {
                var cabRenderer = (Viewer.PlayerLocomotiveViewer as MSTSLocomotiveViewer)._CabRenderer;

                var loco = Viewer.PlayerLocomotive as MSTSLocomotive;
                CabViewFront = !loco.UsingRearCab;

                var itemsFrontCount = loco.CabViewList[(int)CabViewType.Front].CVFFile.CabViewControls.Count();
                var itemsRearCount = loco.CabViewList.Count > 1 ? loco.CabViewList[(int)CabViewType.Rear].CVFFile.CabViewControls.Count() : 0;

                foreach (var controlRenderer in cabRenderer.ControlMap.Values.Skip(CabViewFront ? 0 : itemsFrontCount).Take(CabViewFront ? itemsFrontCount : itemsRearCount))
                {
                    if ((Viewer.Camera as CabCamera).SideLocation == controlRenderer.Control.CabViewpoint && controlRenderer is ICabViewMouseControlRenderer mouseRenderer)
                    {
                        if (mouseRenderer.isMouseControl())
                        {
                            Rectangle rectangle = mouseRenderer.DestinationRectangleGet();
                            int width = rectangle.Width;
                            int height = rectangle.Height;

                            if (width > 0)
                            {
                                // do not know why rectangles with width and height = 0 are there
                                ListRects = ListRectangles.FirstOrDefault(c => c.Name == mouseRenderer.GetControlName() && c.Front == CabViewFront);
                                if (ListRects == null)
                                {
                                    ListRectangles.Add(new ListRect
                                    {
                                        CabRectangle = rectangle,
                                        Front = CabViewFront,
                                        Name = mouseRenderer.GetControlName(),
                                    });
                                }
                                else
                                {
                                    ListRects.CabRectangle = rectangle;
                                }

                                Thickness = 1; // default
                                if (mouseRenderer.IsMouseWithin())
                                {
                                    ListRects = ListRectangles.FirstOrDefault(c => c.CabRectangle == rectangle && c.Front == CabViewFront);

                                    if (ListRects != null && rectangle.Intersects(ListRects.CabRectangle) && ListRects.Name == mouseRenderer.GetControlName() && !IsOverRectangle)
                                    {
                                        Thickness = 3; // Highlights the currently selected rectangle
                                        IsOverRectangle = true;
                                    }
                                }

                                DrawRectangle(spriteBatch, rectangle.X, rectangle.Y, width, height, Thickness, Color);
                            }
                        }
                    }
                }
                IsOverRectangle = false;
            }
        }

        private void DrawRectangle(SpriteBatch spriteBatch, int newX, int newY, int width, int height, int Thickness, Color Color)
        {   // top line
            DrawLine(spriteBatch, newX, newY, width, Thickness, 0, Color);
            // bottom line
            DrawLine(spriteBatch, newX, newY + height - Thickness, width, Thickness, 0, Color);
            // left line
            DrawLine(spriteBatch, newX + Thickness, newY, height, Thickness, 90, Color);
            // right line
            DrawLine(spriteBatch, newX + width, newY, height, Thickness, 90, Color);
        }

        private void DrawLine(SpriteBatch spriteBatch, int X, int Y, int width, int height, int degrees, Color Color)
        {
            spriteBatch.Draw(
                Line,
                new Rectangle(X, Y, width, height),
                null,
                Color,
                ConvertToRadiansFromDegrees(degrees),
                new Vector2(0, 0),
                SpriteEffects.None, 0);
        }

        private float ConvertToRadiansFromDegrees(int angle)
        {
            return (float)((System.Math.PI / 180) * angle);
        }
    }
}
