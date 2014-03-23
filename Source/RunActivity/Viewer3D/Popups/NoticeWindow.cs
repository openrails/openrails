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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Globalization;

namespace ORTS.Viewer3D.Popups
{
    public class NoticeWindow : LayeredWindow
    {
        const double AnimationLength = 0.4;
        const double AnimationFade = 0.1;
        const double NoticeTextSize = 0.1;

        // Screen-related data
        WindowTextFont Font;

        // Updated data
        float FieldOfView;

        // Notice data
        string NoticeText;
        Point NoticeSize;
        bool Animation;
        double AnimationStart = -1;

        public NoticeWindow(WindowManager owner)
            : base(owner, 10, 10, "Field of View")
        {
            Visible = true;
        }

        internal override void ScreenChanged()
        {
            // SizeTo does not clamp the size so we should do it first; MoveTo clamps position.
            SizeTo(Owner.ScreenSize.X, Owner.ScreenSize.Y);
            MoveTo(0, 0);

            Font = Owner.TextManager.Get("Arial", (int)(Owner.ScreenSize.Y * NoticeTextSize), System.Drawing.FontStyle.Regular);

            base.ScreenChanged();
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override bool TopMost
        {
            get
            {
                return true;
            }
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (Owner.Viewer.Camera != null && FieldOfView != Owner.Viewer.Camera.FieldOfView && Owner.Viewer.RealTime > 0.1)
            {
                FieldOfView = Owner.Viewer.Camera.FieldOfView;
                SetNotice(String.Format("{0:F0}°", FieldOfView));
            }
            else if (Animation && Owner.Viewer.RealTime > AnimationStart + AnimationLength)
            {
                Animation = false;
            }
        }

        void SetNotice(string noticeText)
        {
            NoticeText = noticeText;
            NoticeSize = new Point(Font.MeasureString(noticeText), Font.Height);
            Animation = true;
            AnimationStart = Owner.Viewer.RealTime;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Animation)
            {
                var currentLength = Owner.Viewer.RealTime - AnimationStart;
                if (currentLength > AnimationLength)
                    currentLength = AnimationLength;

                var fade = 1.0f;
                if (currentLength < AnimationFade)
                    fade = (float)(currentLength / AnimationFade);
                else if (currentLength > AnimationLength - AnimationFade)
                    fade = (float)((currentLength - AnimationLength) / -AnimationFade);

                var rectangle = new Rectangle(Location.Width / 2, Location.Height / 2, 0, 0);
                rectangle.Inflate(NoticeSize.X / 2, NoticeSize.Y / 2);
                spriteBatch.Draw(WindowManager.NoticeTexture, rectangle, Color.White);
                Font.Draw(spriteBatch, rectangle, Point.Zero, NoticeText, LabelAlignment.Center, Color.White);
            }
        }
    }
}
