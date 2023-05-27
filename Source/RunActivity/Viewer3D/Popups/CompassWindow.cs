// COPYRIGHT 2010, 2011, 2013, 2014, 2015 by the Open Rails project.
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

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using ORTS.Common;

namespace Orts.Viewer3D.Popups
{
    public class CompassWindow : Window
    {
        PopupCompass Compass;
        Label Latitude;
        Label Longitude;

        public CompassWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 15, Window.DecorationSize.Y + owner.TextFontDefault.Height * 4, Viewer.Catalog.GetString("Compass"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            vbox.Add(Compass = new PopupCompass(vbox.RemainingWidth, vbox.RemainingHeight - vbox.TextHeight));
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                var w = hbox.RemainingWidth / 9;
                hbox.Add(new Label(1 * w, hbox.RemainingHeight, Viewer.Catalog.GetString("Lat:"), LabelAlignment.Right));
                hbox.Add(Latitude = new Label(3 * w, hbox.RemainingHeight, "000.000000", LabelAlignment.Right));
                hbox.AddSpace(w, hbox.RemainingHeight);
                hbox.Add(new Label(1 * w, hbox.RemainingHeight, Viewer.Catalog.GetString("Lon:"), LabelAlignment.Right));
                hbox.Add(Longitude = new Label(3 * w, hbox.RemainingHeight, "000.000000", LabelAlignment.Right));
            }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            var camera = Owner.Viewer.Camera;
            var compassDir = new Vector2(camera.XnaView.M11, camera.XnaView.M13);
            var heading = Math.Acos(compassDir.X);
            if (compassDir.Y > 0) heading = 2 * Math.PI - heading;
            Compass.Heading = MathHelper.ToDegrees((float)heading);

            if (updateFull)
            {
                double latitude = 0;
                double longitude = 0;
                new WorldLatLon().ConvertWTC(camera.TileX, camera.TileZ, camera.Location, ref latitude, ref longitude);
                Latitude.Text = MathHelper.ToDegrees((float)latitude).ToString("F6");
                Longitude.Text = MathHelper.ToDegrees((float)longitude).ToString("F6");
            }
        }
    }

    public class PopupCompass : Control
    {
        static Texture2D CompassTexture;
        static int[] HeadingHalfWidths;
        WindowTextFont Font;
        public float Heading;

        public PopupCompass(int width, int height)
            : base(0, 0, width, height)
        {
        }

        public override void Initialize(WindowManager windowManager)
        {
            base.Initialize(windowManager);
            Font = windowManager.TextFontDefault;
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            if (CompassTexture == null)
            {
                CompassTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                CompassTexture.SetData(new[] { Color.White });
            }
            if (HeadingHalfWidths == null)
            {
                HeadingHalfWidths = new int[12];
                for (var i = 0; i < 12; i++)
                    HeadingHalfWidths[i] = Font.MeasureString((i * 30).ToString()) / 2;
            }
            const int headingScale = 2;
            var height = (int)((Position.Height - Font.Height) / 3);
            for (float heading = 0; heading < 360; heading += 10)
            {
                var x = Position.Width / 2 + (int)(((heading - Heading + 360 + 180) % 360 - 180) * headingScale);
                if ((x >= 0) && (x < Position.Width))
                {
                    if (heading % 30 == 0)
                    {
                        var textHalfWidth = HeadingHalfWidths[(int)heading / 30];
                        if ((x - textHalfWidth >= 0) && (x + textHalfWidth < Position.Width))
                            Font.Draw(spriteBatch, new Point(offset.X + Position.X + x - textHalfWidth, offset.Y + Position.Y), heading.ToString(), Color.White);
                        spriteBatch.Draw(CompassTexture, new Rectangle(offset.X + Position.X + x, offset.Y + Position.Y + Font.Height, 1, height * 2), Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(CompassTexture, new Rectangle(offset.X + Position.X + x, offset.Y + Position.Y + Font.Height, 1, height), Color.White);
                    }
                }
            }
            spriteBatch.Draw(CompassTexture, new Rectangle(offset.X + Position.X + Position.Width / 2, offset.Y + Position.Bottom - height, 1, height), Color.White);
        }
    }
}
