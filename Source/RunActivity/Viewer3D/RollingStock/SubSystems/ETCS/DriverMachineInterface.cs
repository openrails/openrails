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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock.SubSystems.ETCS;
using ORTS.Common;
using ORTS.Scripting.Api.ETCS;

namespace Orts.Viewer3D.RollingStock.Subsystems.ETCS
{
    public class DriverMachineInterface
    {
        public readonly MSTSLocomotive Locomotive;
        readonly Viewer Viewer;
        public readonly CircularSpeedGauge CircularSpeedGauge;
        public readonly PlanningWindow PlanningWindow;
        public readonly DistanceArea DistanceArea;
        public readonly MessageArea MessageArea;
        float PrevScale = 1;

        bool Active;

        public bool ShowDistanceAndSpeedInformation;
        public float Scale { get; private set; }
        public float MipMapScale { get; private set; }
        readonly int Height = 480;
        readonly int Width = 640;

        // Color RGB values are from ETCS specification
        public static readonly Color ColorGrey = new Color(195, 195, 195);
        public static readonly Color ColorMediumGrey = new Color(150, 150, 150);
        public static readonly Color ColorDarkGrey = new Color(85, 85, 85);
        public static readonly Color ColorYellow = new Color(223, 223, 0);
        public static readonly Color ColorOrange = new Color(234, 145, 0);
        public static readonly Color ColorRed = new Color(191, 0, 2);
        public static readonly Color ColorBackground = new Color(3, 17, 34); // dark blue
        public static readonly Color ColorPASPlight = new Color(41, 74, 107);
        public static readonly Color ColorPASPdark = new Color(33, 49, 74);

        readonly Point SpeedAreaLocation;
        readonly Point PlanningLocation;
        readonly Point DistanceAreaLocation;
        readonly Point MessageAreaLocation;

        public Texture2D ColorTexture { get; private set; }

        bool DisplayBackground = false;

        public bool Blinker2Hz;
        public bool Blinker4Hz;
        float BlinkerTime;

        /// <summary>
        /// True if the screen is sensitive
        /// </summary>
        public bool IsTouchScreen = true;
        /// <summary>
        /// Controls the layout of the DMI screen depending.
        /// Must be true if there are physical buttons to control the DMI, even if it is a touch screen.
        /// If false, the screen must be tactile.
        /// </summary>
        public bool IsSoftLayout;

        /// <summary>
        /// Class to store information of sensitive areas of the touch screen
        /// </summary>
        public class Button
        {
            public readonly string Name;
            public bool Enabled;
            public readonly bool UpType;
            public readonly Rectangle SensitiveArea;
            public Button(string name, bool upType, Rectangle area)
            {
                Name = name;
                Enabled = false;
                UpType = upType;
                SensitiveArea = area;
            }
        }

        public readonly List<Button> SensitiveButtons = new List<Button>();

        /// <summary>
        /// Name of the button currently being pressed without valid pulsation yet
        /// </summary>
        Button ActiveButton;
        /// <summary>
        /// Name of the button with a valid pulsation in current update cycle
        /// </summary>
        public Button PressedButton;

        public DriverMachineInterface(float height, float width, MSTSLocomotive locomotive, Viewer viewer, CVCDigital control)
        {
            Viewer = viewer;
            Locomotive = locomotive;
            Scale = Math.Min(width / Width, height / Height);

            PlanningLocation = new Point(334, IsSoftLayout ? 0 : 15);
            SpeedAreaLocation = new Point(54, IsSoftLayout ? 0 : 15);
            DistanceAreaLocation = new Point(0, IsSoftLayout ? 0 : 15);
            MessageAreaLocation = new Point(54, IsSoftLayout ? 350 : 365);

            CircularSpeedGauge = new CircularSpeedGauge(
                   (int)control.MaxValue,
                   control.Units == CABViewControlUnits.KM_PER_HOUR,
                   true,
                   control.MaxValue == 240 || control.MaxValue == 260,
                   (int)control.MinValue,
                   Locomotive,
                   Viewer,
                   null,
                   this
               );
            PlanningWindow = new PlanningWindow(this, Viewer, PlanningLocation);
            DistanceArea = new DistanceArea(this, Viewer, DistanceAreaLocation);
            MessageArea = new MessageArea(this, Viewer, MessageAreaLocation);
        }

        public Texture2D LoadTexture(string name)
        {
            if (MipMapScale == 2) return SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "mipmap-2", name));
            else return SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", name));
        }

        public void PrepareFrame(float elapsedSeconds)
        {
            ETCSStatus currentStatus = Locomotive.TrainControlSystem.ETCSStatus;
            Active = currentStatus != null && currentStatus.DMIActive;
            if (!Active) return;

            BlinkerTime += elapsedSeconds;
            BlinkerTime -= (int)BlinkerTime;
            Blinker2Hz = BlinkerTime < 0.5;
            Blinker4Hz = BlinkerTime < 0.25 || (BlinkerTime > 0.5 && BlinkerTime < 0.75);

            CircularSpeedGauge.PrepareFrame(currentStatus);
            PlanningWindow.PrepareFrame(currentStatus);
            DistanceArea.PrepareFrame(currentStatus);
            MessageArea.PrepareFrame(currentStatus);
        }
        public void SizeTo(float width, float height)
        {
            Scale = Math.Min(width / Width, height / Height);

            if (Math.Abs(1f - PrevScale / Scale) > 0.1f)
            {
                PrevScale = Scale;
                if (Scale < 0.5) MipMapScale = 2;
                else MipMapScale = 1;
                CircularSpeedGauge.ScaleChanged();
                PlanningWindow.ScaleChanged();
                DistanceArea.ScaleChanged();
                MessageArea.ScaleChanged();
            }
        }

        public void Draw(SpriteBatch spriteBatch, Point position)
        {
            if (ColorTexture == null)
            {
                ColorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                ColorTexture.SetData(new[] { Color.White });
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap);
            if (!Active) return;
            if (DisplayBackground) spriteBatch.Draw(ColorTexture, new Rectangle(position, new Point((int)(640 * Scale), (int)(480 * Scale))), ColorBackground);
            CircularSpeedGauge.Draw(spriteBatch, new Point(position.X + (int)(SpeedAreaLocation.X * Scale), position.Y + (int)(SpeedAreaLocation.Y * Scale)));
            PlanningWindow.Draw(spriteBatch, new Point(position.X + (int)(PlanningLocation.X * Scale), position.Y + (int)(PlanningLocation.Y * Scale)));
            DistanceArea.Draw(spriteBatch, new Point(position.X + (int)(DistanceAreaLocation.X * Scale), position.Y + (int)(DistanceAreaLocation.Y * Scale)));
            MessageArea.Draw(spriteBatch, new Point(position.X + (int)(MessageAreaLocation.X * Scale), position.Y + (int)(MessageAreaLocation.Y * Scale)));
        }

        public void HandleMouseInput(bool pressed, int x, int y)
        {
            PressedButton = null;
            if (ActiveButton != null)
            {
                if (!pressed && ActiveButton.Enabled && ActiveButton.UpType && ActiveButton.SensitiveArea.Contains(x, y))
                {
                    PressedButton = ActiveButton;
                }
            }
            else if (pressed)
            {
                foreach (Button b in SensitiveButtons)
                {
                    if (b.SensitiveArea.Contains(x, y))
                    {
                        ActiveButton = b;
                        if (!b.UpType && b.Enabled) PressedButton = ActiveButton;
                        break;
                    }
                }
            }
            if (!pressed) ActiveButton = null;
            if (PressedButton != null)
            {
                PlanningWindow.HandleInput();
                MessageArea.HandleInput();
                PressedButton = null;
            }
        }
        /*public void HandleButtonInput(string button, bool pressed)
        {
            if (pressed)
            {
                if ()
            }
            else if (ActiveButton == button && SensitiveButtons[ActiveButton].UpType)
            {
                PressedButton = ActiveButton;
                ActiveButton = null;
            }
        }*/
    }

    public abstract class DMIWindow
    {
        protected readonly DriverMachineInterface DMI;
        public float Scale => DMI.Scale;
        protected Texture2D ColorTexture => DMI.ColorTexture;
        public class TextPrimitive
        {
            public Point Position;
            public Color Color;
            public WindowTextFont Font;
            public string Text;

            public TextPrimitive(Point position, Color color, string text, WindowTextFont font)
            {
                Position = position;
                Color = color;
                Text = text;
                Font = font;
            }

            public void Draw(SpriteBatch spriteBatch, Point position)
            {
                Font.Draw(spriteBatch, position, Text, Color);
            }
        }
        protected DMIWindow(DriverMachineInterface dmi)
        {
            DMI = dmi;
        }

        public abstract void PrepareFrame(ETCSStatus status);
        public abstract void Draw(SpriteBatch spriteBatch, Point position);
        public Rectangle ScaledRectangle(Point origin, int x, int y, int width, int height)
        {
            return new Rectangle(origin.X + (int)(x * Scale), origin.Y + (int)(y * Scale), Math.Max((int)(width * Scale), 1), Math.Max((int)(height * Scale), 1));
        }
        public void DrawSymbol(SpriteBatch spriteBatch, Texture2D texture, Point origin, float x, float y)
        {
            spriteBatch.Draw(texture, new Vector2(origin.X + x * Scale, origin.Y + y * Scale), null, Color.White, 0, Vector2.Zero, Scale * DMI.MipMapScale, SpriteEffects.None, 0);
        }
        /// <summary>
        /// Get scaled font size, increasing it if result is small
        /// </summary>
        /// <param name="requiredSize"></param>
        /// <returns></returns>
        public float GetScaledFontSize(float requiredSize)
        {
            float size = requiredSize * Scale;
            if (size < 5) return size * 1.2f;
            return size;
        }
    }
    public class DriverMachineInterfaceRenderer : CabViewDigitalRenderer, ICabViewMouseControlRenderer
    {
        DriverMachineInterface DMI;
        [CallOnThread("Loader")]
        public DriverMachineInterfaceRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCDigital control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            // Height is adjusted to keep compatibility with existing gauges
            DMI = new DriverMachineInterface((int)(Control.Width * 640 / 280), (int)(Control.Height * 480 / 300), locomotive, viewer, control);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            base.PrepareFrame(frame, elapsedTime);
            DMI.PrepareFrame(elapsedTime.ClockSeconds);
            DMI.SizeTo(DrawPosition.Width * 640 / 280, DrawPosition.Height * 480 / 300);
        }

        public bool IsMouseWithin()
        {
            int x = (int)((UserInput.MouseX - DrawPosition.X) / DMI.Scale + 54);
            int y = (int)((UserInput.MouseY - DrawPosition.Y) / DMI.Scale + 15);
            foreach (DriverMachineInterface.Button b in DMI.SensitiveButtons)
            {
                if (b.SensitiveArea.Contains(x, y) && b.Enabled) return true;
            }
            return false;
        }

        public void HandleUserInput()
        {
            DMI.HandleMouseInput(UserInput.IsMouseLeftButtonDown, (int)((UserInput.MouseX - DrawPosition.X) / DMI.Scale + 54), (int)((UserInput.MouseY - DrawPosition.Y) / DMI.Scale + 15));
        }

        public string GetControlName()
        {
            int x = (int)((UserInput.MouseX - DrawPosition.X) / DMI.Scale + 54);
            int y = (int)((UserInput.MouseY - DrawPosition.Y) / DMI.Scale + 15);
            foreach (DriverMachineInterface.Button b in DMI.SensitiveButtons)
            {
                if (b.SensitiveArea.Contains(x, y)) return b.Name;
            }
            return "";
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            DMI.Draw(CabShaderControlView.SpriteBatch, new Point((int)(DrawPosition.X - 54 * DMI.Scale), (int)(DrawPosition.Y - 15 * DMI.Scale)));
            CabShaderControlView.SpriteBatch.End();
            CabShaderControlView.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, Shader);
        }
    }
}