// COPYRIGHT 2011 by the Open Rails project.
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
    public class PauseWindow : LayeredWindow
    {
        const double AnimationLength = 0.4;
        const double AnimationFade = 0.1;
        const double AnimationSize = 0.2;

        bool GamePaused;
        bool Animation;
        double AnimationStart = -1;
        Rectangle AnimationSource;

        public PauseWindow(WindowManager owner)
            : base(owner, 10, 10, "Pause")
        {
            Visible = true;
        }

        internal override void ScreenChanged()
        {
            // SizeTo does not clamp the size so we should do it first; MoveTo clamps position.
            SizeTo(Owner.ScreenSize.X, Owner.ScreenSize.Y);
            MoveTo(0, 0);

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

            if (GamePaused != Owner.Viewer.Simulator.Paused && Owner.Viewer.RealTime > 0.1)
            {
                GamePaused = Owner.Viewer.Simulator.Paused;
                Animation = true;
                AnimationStart = Owner.Viewer.RealTime;
                AnimationSource = new Rectangle(0, 0, WindowManager.PauseTexture.Width, WindowManager.PauseTexture.Height / 2);
                if (GamePaused)
                    AnimationSource.Y += AnimationSource.Height;
            }
            else if (Animation && Owner.Viewer.RealTime > AnimationStart + AnimationLength)
            {
                Animation = false;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Animation)
            {
                var currentLength = Owner.Viewer.RealTime - AnimationStart;
                if (currentLength > AnimationLength)
                    currentLength = AnimationLength;

                var currentSize = (int)(AnimationSize * Location.Height * (1 - 0.5 / (1 + currentLength)));

                var fade = 1.0f;
                if (currentLength < AnimationFade)
                    fade = (float)(currentLength / AnimationFade);
                else if (currentLength > AnimationLength - AnimationFade)
                    fade = (float)((currentLength - AnimationLength) / -AnimationFade);

                var rectangle = new Rectangle(Location.Width / 2, Location.Height / 2, 0, 0);
                rectangle.Inflate(currentSize, currentSize);
                spriteBatch.Draw(WindowManager.PauseTexture, rectangle, AnimationSource, new Color(Color.White, fade));
            }
        }
    }
}
