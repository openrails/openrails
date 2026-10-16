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
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock.SubSystems.ETCS;
using ORTS.Common;
using ORTS.Scripting.Api.ETCS;
using static Orts.Viewer3D.RollingStock.Subsystems.ETCS.DriverMachineInterface;

namespace Orts.Viewer3D.RollingStock.Subsystems.ETCS
{
    public class DriverMachineInterface
    {
        public readonly MSTSLocomotive Locomotive;
        public readonly Viewer Viewer;
        public IList<DMIWindow> Windows = new List<DMIWindow>();
        float PrevScale = 1;
        public ETCSStatus ETCSStatus { get; private set; }

        bool Active;
        public float Scale { get; private set; }
        public float MipMapScale { get; private set; }
        public readonly int Height;
        public readonly int Width;

        public readonly ETCSDefaultWindow ETCSDefaultWindow;

        public readonly DriverMachineInterfaceShader Shader;

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
        public static readonly Color ColorShadow = new Color(8, 24, 57);

        // Some DMIs use black for the background and white for borders, instead of blue scale
        public readonly bool BlackWhiteTheme = false;

        public Texture2D ColorTexture { get; private set; }

        public bool Blinker2Hz { get; private set; }
        public bool Blinker4Hz { get; private set; }

        public enum DMIMode
        {
            FullSize,
            SpeedArea,
            PlanningArea,
            GaugeOnly
        }
        public DMIMode CurrentDMIMode;

        float BlinkerTime;

        public float CurrentTime => (float)Viewer.Simulator.ClockTime;

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
        public DMIWindow ActiveWindow;
        DMIButton ActiveButton;

        public static readonly Dictionary<DMIMode, (int Width, int Height)> ScreenSizes = new Dictionary<DMIMode, (int, int)>
        {
            { DMIMode.FullSize, (640, 480) },
            { DMIMode.SpeedArea, (334, 480) },
            { DMIMode.PlanningArea, (334, 480) },
            { DMIMode.GaugeOnly, (280, 300) },
        };

        public DriverMachineInterface(MSTSLocomotive locomotive, Viewer viewer, CabViewControl control)
        {
            if (!(control is CVCScreen cvcScreen))
                CurrentDMIMode = DMIMode.GaugeOnly;
            else if (!(cvcScreen.CustomParameters.TryGetValue("mode", out var mode) && Enum.TryParse(mode, ignoreCase: true, out CurrentDMIMode))) 
                CurrentDMIMode = DMIMode.FullSize;

            Width = ScreenSizes[CurrentDMIMode].Width;
            Height = ScreenSizes[CurrentDMIMode].Height;

            SizeTo((float)control.Width, (float)control.Height);

            Viewer = viewer;
            Locomotive = locomotive;
            Shader = new DriverMachineInterfaceShader(Viewer.GraphicsDevice);

            ActiveWindow = ETCSDefaultWindow = new ETCSDefaultWindow(this, control) { Visible = true };
            AddToLayout(ETCSDefaultWindow, Point.Zero);
        }

        public void ShowSubwindow(DMISubwindow window)
        {
            AddToLayout(window, new Point(window.FullScreen ? 0 : 334, 15));
        }
        public void AddToLayout(DMIWindow window, Point position)
        {
            window.Position = position;
            window.Parent = ActiveWindow;
            ActiveWindow = window;
            Windows.Add(window);
        }
        public Texture2D LoadTexture(string name)
        {
            string path;
            if (MipMapScale == 2)
                path = System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "mipmap-2", name);
            else
                path = System.IO.Path.Combine(Viewer.ContentPath, "ETCS", name);
            return SharedTextureManager.LoadInternal(Viewer.RenderProcess.GraphicsDevice, path);
        }
        public void PrepareFrame(float elapsedSeconds)
        {
            ETCSStatus currentStatus = Locomotive.TrainControlSystem.ETCSStatus;
            ETCSStatus = currentStatus;
            Active = currentStatus != null && currentStatus.DMIActive;
            if (!Active) return;

            BlinkerTime += elapsedSeconds;
            BlinkerTime -= (int)BlinkerTime;
            Blinker2Hz = BlinkerTime < 0.5;
            Blinker4Hz = BlinkerTime < 0.25 || (BlinkerTime > 0.5 && BlinkerTime < 0.75);

            foreach (var area in Windows)
            {
                area.PrepareFrame(currentStatus);
            }
        }
        public void SizeTo(float width, float height)
        {
            Scale = width != 0 && height != 0 ? Math.Min(width / Width, height / Height) : 1;

            if (Math.Abs(1f - PrevScale / Scale) > 0.1f)
            {
                PrevScale = Scale;
                if (Scale < 0.5) MipMapScale = 2;
                else MipMapScale = 1;
                foreach (var area in Windows)
                {
                    area.ScaleChanged();
                }
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
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null); // TODO: Handle brightness via DMI shader
            if (!Active) return;
            foreach (var area in Windows)
            {
                area.Draw(spriteBatch, new Point(position.X + (int)(area.Position.X * Scale), position.Y + (int)(area.Position.Y * Scale)));
            }
        }

        public void HandleMouseInput(bool pressed, int x, int y)
        {
            DMIButton pressedButton = null;
            if (ActiveButton != null)
            {
                if (!ActiveButton.Enabled)
                {
                    ActiveButton.Pressed = false;
                    ActiveButton = null;
                }
                else if (ActiveButton.SensitiveArea(ActiveWindow.Position).Contains(x, y))
                {
                    if (ActiveButton.UpType)
                    {
                        if (ActiveButton.DelayType && ActiveButton.FirstPressed + 2 > CurrentTime)
                        {
                            ActiveButton.Pressed = ((int)((CurrentTime - ActiveButton.FirstPressed) * 4)) % 2 == 0;
                        }
                        else
                        {
                            ActiveButton.Pressed = true;
                            if (!pressed)
                            {
                                pressedButton = ActiveButton;
                            }
                        }
                    }
                    else
                    {
                        ActiveButton.Pressed = false;
                        if (ActiveButton.FirstPressed + 1.5 < CurrentTime)
                        {
                            if (ActiveButton.LastPressed + 0.3 < CurrentTime)
                            {
                                pressedButton = ActiveButton;
                                ActiveButton.Pressed = true;
                                ActiveButton.LastPressed = CurrentTime;
                            }
                        }
                    }
                }
                else
                {
                    ActiveButton.FirstPressed = CurrentTime;
                    ActiveButton.Pressed = false;
                }
            }
            else if (pressed)
            {
                foreach (var area in ActiveWindow.SubAreas)
                {
                    if (!(area is DMIButton)) continue;
                    var b = (DMIButton)area;
                    b.Pressed = false;
                    if (b.SensitiveArea(ActiveWindow.Position).Contains(x, y))
                    {
                        ActiveButton = b;
                        ActiveButton.Pressed = true;
                        ActiveButton.FirstPressed = CurrentTime;
                        if (!b.UpType && b.Enabled) pressedButton = ActiveButton;
                        break;
                    }
                }
            }
            if (!pressed && ActiveButton != null)
            {
                ActiveButton.Pressed = false;
                ActiveButton = null;
            }
            pressedButton?.PressedAction();
        }
        public void ExitWindow(DMIWindow window)
        {
            var windows = new List<DMIWindow>(Windows);
            windows.Remove(window);
            Windows = windows;
            if (window.Parent == null) ActiveWindow = ETCSDefaultWindow;
            else ActiveWindow = window.Parent;
        }
    }
    public class ETCSDefaultWindow : DMIWindow
    {
        CircularSpeedGauge CircularSpeedGauge;
        PlanningWindow PlanningWindow;
        MessageArea MessageArea;
        TargetDistance TargetDistance;
        TTIandLSSMArea TTIandLSSMArea;
        MenuBar MenuBar;
        public ETCSDefaultWindow(DriverMachineInterface dmi, CabViewControl control) : base(dmi, dmi.Width, dmi.Height)
        {
            if (dmi.CurrentDMIMode == DMIMode.GaugeOnly)
            {
                var dig = control as CVCDigital;
                CircularSpeedGauge = new CircularSpeedGauge(
                    (int)dig.MaxValue,
                    dig.Units != CABViewControlUnits.MILES_PER_HOUR,
                    dig.Units != CABViewControlUnits.NONE,
                    dig.MaxValue == 240 || dig.MaxValue == 260,
                    (int)dig.MinValue,
                    DMI);
                AddToLayout(CircularSpeedGauge, new Point(0, 0));
                return;
            }
            if (dmi.CurrentDMIMode != DMIMode.PlanningArea)
            {
                var param = (control as CVCScreen).CustomParameters;
                int maxSpeed = 400;
                if (param.ContainsKey("maxspeed")) int.TryParse(param["maxspeed"], out maxSpeed);
                int maxVisibleSpeed = maxSpeed;
                if (param.ContainsKey("maxvisiblespeed")) int.TryParse(param["maxvisiblespeed"], out maxVisibleSpeed);
                CircularSpeedGauge = new CircularSpeedGauge(
                       maxSpeed,
                       control.Units != CABViewControlUnits.MILES_PER_HOUR,
                       param.ContainsKey("displayunits") && param["displayunits"] == "1",
                       maxSpeed == 240 || maxSpeed == 260,
                       maxVisibleSpeed,
                       dmi
                );
                TTIandLSSMArea = new TTIandLSSMArea(dmi);
                TargetDistance = new TargetDistance(dmi);
                MessageArea = new MessageArea(dmi);
                CircularSpeedGauge.Layer = -1;
                TargetDistance.Layer = -1;
                TTIandLSSMArea.Layer = -1;
                MessageArea.Layer = -1;
                AddToLayout(CircularSpeedGauge, new Point(54, DMI.IsSoftLayout ? 0 : 15));
                AddToLayout(TTIandLSSMArea, new Point(0, DMI.IsSoftLayout ? 0 : 15));
                AddToLayout(TargetDistance, new Point(0, 54 + (DMI.IsSoftLayout ? 0 : 15)));
                AddToLayout(MessageArea, new Point(54, DMI.IsSoftLayout ? 350 : 365));
                AddToLayout(MessageArea.ButtonScrollUp, new Point(54 + 234, DMI.IsSoftLayout ? 350 : 365));
                AddToLayout(MessageArea.ButtonScrollDown, new Point(54 + 234, MessageArea.Height / 2 + (DMI.IsSoftLayout ? 350 : 365)));
            }
            if (dmi.CurrentDMIMode != DMIMode.SpeedArea)
            {
                // Calculate start position of the planning area when a two-screen display is used
                // Real width of the left area in ETCS specs is 306 px, however in order to have
                // both screens with the same size I assumed both have 334 px
                // To be checked
                int startPos = dmi.CurrentDMIMode == DMIMode.FullSize ? 334 : (334-306)/2;
                PlanningWindow = new PlanningWindow(dmi);
                MenuBar = new MenuBar(dmi);
                AddToLayout(PlanningWindow, new Point(startPos, DMI.IsSoftLayout ? 0 : 15));
                AddToLayout(PlanningWindow.ButtonScaleDown, new Point(startPos, DMI.IsSoftLayout ? 0 : 15));
                AddToLayout(PlanningWindow.ButtonScaleUp, new Point(startPos, 285 + (DMI.IsSoftLayout ? 0 : 15)));
                foreach (int i in Enumerable.Range(0, MenuBar.Buttons.Count))
                {
                    AddToLayout(MenuBar.Buttons[i], new Point(580, 15 + 50 * i));
                }
            }
        }
    }

    public class DMIArea
    {
        public Point Position;
        public readonly DriverMachineInterface DMI;
        protected Texture2D ColorTexture => DMI.ColorTexture;
        public float Scale => DMI.Scale;
        public readonly int Height;
        public readonly int Width;
        protected List<RectanglePrimitive> Rectangles = new List<RectanglePrimitive>();
        protected List<TextPrimitive> Texts = new List<TextPrimitive>();
        protected List<TexturePrimitive> Textures = new List<TexturePrimitive>();
        public int Layer;
        protected bool FlashingFrame;
        public Color BackgroundColor = Color.Transparent;
        public bool Pressed;
        public bool Visible;
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
        public struct TexturePrimitive
        {
            public readonly Texture2D Texture;
            public readonly Vector2 Position;
            public TexturePrimitive(Texture2D texture, Vector2 position)
            {
                Texture = texture;
                Position = position;
            }
            public TexturePrimitive(Texture2D texture, float x, float y)
            {
                Texture = texture;
                Position = new Vector2(x, y);
            }
        }
        public struct RectanglePrimitive
        {
            public readonly float X;
            public readonly float Y;
            public readonly float Width;
            public readonly float Height;
            public readonly bool DrawAsInteger;
            public Color Color;
        }
        public DMIArea(DriverMachineInterface dmi)
        {
            DMI = dmi;
        }
        public DMIArea(DriverMachineInterface dmi, int width, int height)
        {
            DMI = dmi;
            Width = width;
            Height = height;
        }
        public virtual void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (BackgroundColor != Color.Transparent) DrawRectangle(spriteBatch, drawPosition, 0, 0, Width, Height, BackgroundColor);

            foreach (var r in Rectangles)
            {
                if (r.DrawAsInteger) DrawIntRectangle(spriteBatch, drawPosition, r.X, r.Y, r.Width, r.Height, r.Color);
                else DrawRectangle(spriteBatch, drawPosition, r.X, r.Y, r.Width, r.Height, r.Color);
            }
            foreach(var text in Texts)
            {
                int x = drawPosition.X + (int)Math.Round(text.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
            foreach(var tex in Textures)
            {
                DrawSymbol(spriteBatch, tex.Texture, drawPosition, tex.Position.Y, tex.Position.Y);
            }
            if (FlashingFrame && DMI.Blinker4Hz)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 2, Height, ColorYellow);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 2, 0, 2, Height, ColorYellow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 2, ColorYellow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 2, Width, 2, ColorYellow);
            }
            else if (DMI.BlackWhiteTheme)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.White);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, Color.White);
            }
            else if (this is DMIButton && (this as DMIButton).ShowButtonBorder)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorShadow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorShadow);

                if (!Pressed)
                {
                    DrawIntRectangle(spriteBatch, drawPosition, 1, 1, 1, Height - 2, ColorShadow);
                    DrawIntRectangle(spriteBatch, drawPosition, Width - 2, 1, 1, Height - 2, Color.Black);
                    DrawIntRectangle(spriteBatch, drawPosition, 1, 1, Width - 2, 1, ColorShadow);
                    DrawIntRectangle(spriteBatch, drawPosition, 1, Height - 2, Width - 2, 1, Color.Black);
                }
            }
            else if (Layer < 0)
            {
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorShadow);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, Color.Black);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorShadow);
            }
        }
        public virtual void PrepareFrame(ETCSStatus status) { }

        public void DrawRectangle(SpriteBatch spriteBatch, Point drawPosition, float x, float y, float width, float height, Color color)
        {
            spriteBatch.Draw(ColorTexture, new Vector2(drawPosition.X + x * Scale, drawPosition.Y + y * Scale), null, color, 0f, Vector2.Zero, new Vector2(width * Scale, height * Scale), SpriteEffects.None, 0);
        }
        public void DrawIntRectangle(SpriteBatch spriteBatch, Point drawPosition, float x, float y, float width, float height, Color color)
        {
            spriteBatch.Draw(ColorTexture, new Rectangle(drawPosition.X + (int)(x * Scale), drawPosition.Y + (int)(y * Scale), Math.Max((int)(width * Scale), 1), Math.Max((int)(height * Scale), 1)), null, color);
        }
        public void DrawSymbol(SpriteBatch spriteBatch, Texture2D texture, Point origin, float x, float y)
        {
            spriteBatch.Draw(texture, new Vector2(origin.X + x * Scale, origin.Y + y * Scale), null, Color.White, 0, Vector2.Zero, Scale * DMI.MipMapScale, SpriteEffects.None, 0);
        }
        public WindowTextFont GetFont(float size, bool bold=false)
        {
            return DMI.Viewer.WindowManager.TextManager.GetExact("Arial", GetScaledFontSize(size), bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
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
        public virtual void ScaleChanged() { }
    }
    public class DMIWindow : DMIArea
    {
        public DMIWindow Parent;
        public List<DMIArea> SubAreas = new List<DMIArea>();
        public bool FullScreen;
        protected DMIWindow(DriverMachineInterface dmi, int width, int height) : base(dmi, width, height)
        {
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            if (!Visible) return;
            base.PrepareFrame(status);
            foreach(var area in SubAreas)
            {
                area.PrepareFrame(status);
            }
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, drawPosition);
            foreach(var area in SubAreas)
            {
                if (area.Visible) area.Draw(spriteBatch, new Point((int)Math.Round(drawPosition.X + area.Position.X * Scale), (int)Math.Round(drawPosition.Y + area.Position.Y * Scale)));
            }
        }
        public void AddToLayout(DMIArea area, Point position)
        {
            area.Position = position;
            area.Visible = true;
            SubAreas.Add(area);
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            foreach (var area in SubAreas)
            {
                area.ScaleChanged();
            }
        }
    }
    public class DMISubwindow : DMIWindow
    {
        public string WindowTitle { get; private set; }
        TextPrimitive WindowTitleText;
        WindowTextFont WindowTitleFont;
        readonly int FontHeightWindowTitle = 12;
        protected readonly DMIIconButton CloseButton;
        public DMISubwindow(string title, bool fullScreen, DriverMachineInterface dmi) : base(dmi, fullScreen ? 640 : 306, 450)
        {
            WindowTitle = title;
            FullScreen = fullScreen;
            CloseButton = new DMIIconButton("NA_11.bmp", "NA_12.bmp", Viewer.Catalog.GetString("Close"), true, () => dmi.ExitWindow(this), 82, 50, dmi);
            CloseButton.Enabled = true;
            BackgroundColor = DMI.BlackWhiteTheme ? Color.Black : ColorBackground;
            SetFont();
            AddToLayout(CloseButton, new Point(fullScreen ? 334 : 0, 400));
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetFont();
        }
        void SetFont()
        {
            WindowTitleFont = GetFont(FontHeightWindowTitle);
            SetTitle(WindowTitle);
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            if (!Visible) return;
            base.Draw(spriteBatch, drawPosition);
            DrawRectangle(spriteBatch, drawPosition, 0, 0, FullScreen ? 334 : 306, 24, Color.Black);
            int x = drawPosition.X + (int)Math.Round(WindowTitleText.Position.X * Scale);
            int y = drawPosition.Y + (int)Math.Round(WindowTitleText.Position.Y * Scale);
            WindowTitleText.Draw(spriteBatch, new Point(x, y));
        }
        public void SetTitle(string s)
        {
            WindowTitle = s;
            int length = (int)(WindowTitleFont.MeasureString(s) / Scale);
            int x = FullScreen ? (334 - length - 5) : 5;
            WindowTitleText = new TextPrimitive(new Point(x, (24-FontHeightWindowTitle)/2), ColorGrey, WindowTitle, WindowTitleFont);
        }
    }
    public class DMIButton : DMIArea
    {
        public Rectangle SensitiveArea(Point WindowPosition) => new Rectangle(WindowPosition.X + Position.X - ExtendedSensitiveArea.X, WindowPosition.Y + Position.Y - ExtendedSensitiveArea.Y, Width + ExtendedSensitiveArea.Width + ExtendedSensitiveArea.X, Height + ExtendedSensitiveArea.Height + ExtendedSensitiveArea.Y);
        public Rectangle ExtendedSensitiveArea;
        public Action PressedAction = null;
        public string ConfirmerCaption;
        public readonly string DisplayName;
        public bool Enabled;
        public bool PressedEffect;
        public readonly bool UpType;
        public bool DelayType;
        public bool ShowButtonBorder;
        public float FirstPressed;
        public float LastPressed;
        public DMIButton(string displayName, bool upType, Action pressedAction, int width, int height, DriverMachineInterface dmi, bool showButtonBorder=false) : base(dmi, width, height)
        {
            DisplayName = displayName;
            Enabled = false;
            UpType = upType;
            PressedAction = pressedAction;
            ShowButtonBorder = showButtonBorder;
        }
    }
    public class DMITextButton : DMIButton
    {
        string[] Caption;
        WindowTextFont CaptionFont;
        int FontHeightButton = 12;
        TextPrimitive[] CaptionText;
        public DMITextButton(string caption, string displayName, bool upType, Action pressedAction, int width, int height, DriverMachineInterface dmi, int fontHeight = 12) :
            base(displayName, upType, pressedAction, width, height, dmi, true)
        {
            Caption = caption.Split('\n');
            CaptionText = new TextPrimitive[Caption.Length];
            ConfirmerCaption = caption;
            FontHeightButton = fontHeight;
            SetFont();
            SetText();
        }
        void SetText()
        {
            foreach (int i in Enumerable.Range(0, Caption.Length))
            {
                int fontWidth = (int)(CaptionFont.MeasureString(Caption[i]) / Scale);
                CaptionText[i] = new TextPrimitive(new Point((Width - fontWidth) / 2, (Height - FontHeightButton) / 2 + FontHeightButton * (2 * i - Caption.Length + 1)), Color.White, Caption[i], CaptionFont);
            }
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetFont();
            SetText();
        }
        void SetFont()
        {
            CaptionFont = GetFont(FontHeightButton);
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            base.PrepareFrame(status);
            foreach (var text in CaptionText)
                text.Color = Enabled ? ColorGrey : ColorDarkGrey;
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            foreach (var text in CaptionText)
            {
                int x = drawPosition.X + (int)Math.Round(text.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
        }
    }
    public class DMIIconButton : DMIButton
    {
        readonly string DisabledSymbol;
        readonly string EnabledSymbol;
        TexturePrimitive DisabledTexture;
        TexturePrimitive EnabledTexture;
        public DMIIconButton(string enabledSymbol, string disabledSymbol, string displayName, bool upType , Action pressedAction, int width, int height, DriverMachineInterface dmi) :
            base(displayName, upType, pressedAction, width, height, dmi, true)
        {
            DisabledSymbol = disabledSymbol;
            EnabledSymbol = enabledSymbol;
            SetIcon();
        }
        void SetIcon()
        {
            Texture2D tex1 = DMI.LoadTexture(EnabledSymbol);
            Texture2D tex2 = DMI.LoadTexture(DisabledSymbol);
            EnabledTexture = new TexturePrimitive(tex1, new Vector2((Width - tex1.Width * DMI.MipMapScale) / 2, (Height - tex1.Height * DMI.MipMapScale) / 2));
            DisabledTexture = new TexturePrimitive(tex2, new Vector2((Width - tex2.Width * DMI.MipMapScale) / 2, (Height - tex2.Height * DMI.MipMapScale) / 2));
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetIcon();
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            var tex = Enabled ? EnabledTexture : DisabledTexture;
            DrawSymbol(spriteBatch, tex.Texture, drawPosition, tex.Position.X, tex.Position.Y);
        }
    }
    public class DMITextLabel : DMIArea
    {
        string[] Caption;
        WindowTextFont CaptionFont;
        int FontHeightButton = 12;
        TextPrimitive[] CaptionText;
        public DMITextLabel(string caption, int width, int height, DriverMachineInterface dmi) :
            base(dmi, width, height)
        {
            Caption = caption.Split('\n');
            CaptionText = new TextPrimitive[Caption.Length];
            SetFont();
            SetText();
        }
        void SetText()
        {
            foreach (int i in Enumerable.Range(0, Caption.Length))
            {
                int fontWidth = (int)(CaptionFont.MeasureString(Caption[i]) / Scale);
                CaptionText[i] = new TextPrimitive(new Point((Width - fontWidth) / 2, (Height - FontHeightButton) / 2 + FontHeightButton * (2 * i - Caption.Length + 1)), ColorGrey, Caption[i], CaptionFont);
            }
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            SetFont();
            SetText();
        }
        void SetFont()
        {
            CaptionFont = GetFont(FontHeightButton);
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            foreach (var text in CaptionText)
            {
                int x = drawPosition.X + (int)Math.Round(text.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }
        }
    }

    public class CircularSpeedGaugeRenderer : CabViewDigitalRenderer
    {
        public DriverMachineInterface DMI;

        [CallOnThread("Loader")]
        public CircularSpeedGaugeRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCDigital control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            // Height is adjusted to keep compatibility
            DMI = new DriverMachineInterface(locomotive, viewer, control);
        }
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            base.PrepareFrame(frame, elapsedTime);
            DMI.SizeTo(DrawPosition.Width, DrawPosition.Height);
            DMI.ETCSDefaultWindow.BackgroundColor = Color.Transparent;
            DMI.PrepareFrame(elapsedTime.ClockSeconds);
        }
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            DMI.Draw(ControlView.SpriteBatch, new Point(DrawPosition.X, DrawPosition.Y));
            ControlView.SpriteBatch.End();
            ControlView.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, Shader);
        }

        public Rectangle DestinationRectangleGet()
        {
            return DrawPosition;
        }
        public bool isMouseControl()
        {
            return true;
        }
    }
    public class DriverMachineInterfaceRenderer : CabViewControlRenderer, ICabViewMouseControlRenderer
    {
        DriverMachineInterface DMI;
        bool Zoomed = false;
        protected Rectangle DrawPosition;
        bool IsTexture3D;

        [CallOnThread("Loader")]
        public DriverMachineInterfaceRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCScreen control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            Position.X = (float)Control.PositionX;
            Position.Y = (float)Control.PositionY;
            if ((int)Control.Height == 102 && (int)Control.Width == 136)
            {
                // Hack for ETR400 cab, which was built with a bugged size calculation of digital displays
                Control.Height *= 0.75f;
                Control.Width *= 0.75f;
            }
            DMI = new DriverMachineInterface(locomotive, viewer, control);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!IsPowered && Control.HideIfDisabled)
                return;

            if (!IsTexture3D)
            {
                base.PrepareFrame(frame, elapsedTime);
                var xScale = (float)Viewer.CabWidthPixels / 640;
                var yScale = (float)Viewer.CabHeightPixels / 480;
                DrawPosition.X = (int)(Position.X * xScale) - Viewer.CabXOffsetPixels + Viewer.CabXLetterboxPixels;
                DrawPosition.Y = (int)(Position.Y * yScale) + Viewer.CabYOffsetPixels + Viewer.CabYLetterboxPixels;
                DrawPosition.Width = (int)(Control.Width * xScale);
                DrawPosition.Height = (int)(Control.Height * yScale);
                if (Zoomed)
                {
                    DrawPosition.Width = DMI.Width;
                    DrawPosition.Height = DMI.Height;
                    DMI.SizeTo(DrawPosition.Width, DrawPosition.Height);
                    DrawPosition.X -= DMI.Width / 2;
                    DrawPosition.Y -= DMI.Height / 2;
                    DMI.ETCSDefaultWindow.BackgroundColor = ColorBackground;
                }
                else
                {
                    DMI.SizeTo(DrawPosition.Width, DrawPosition.Height);
                    DMI.ETCSDefaultWindow.BackgroundColor = Color.Transparent;
                }
            }
            DMI.PrepareFrame(elapsedTime.ClockSeconds);
        }

        public bool IsMouseWithin()
        {
            int x = (int)((UserInput.MouseX - DrawPosition.X) / DMI.Scale);
            int y = (int)((UserInput.MouseY - DrawPosition.Y) / DMI.Scale);
            if (UserInput.IsMouseRightButtonPressed && new Rectangle(0, 0, DMI.Width, DMI.Height).Contains(x, y)) Zoomed = !Zoomed;
            foreach (var area in DMI.ActiveWindow.SubAreas)
            {
                if (!(area is DMIButton)) continue;
                var b = (DMIButton)area;
                if (b.SensitiveArea(DMI.ActiveWindow.Position).Contains(x, y) && b.Enabled) return true;
            }
            return false;
        }

        public void HandleUserInput()
        {
            DMI.HandleMouseInput(UserInput.IsMouseLeftButtonDown, (int)((UserInput.MouseX - DrawPosition.X) / DMI.Scale), (int)((UserInput.MouseY - DrawPosition.Y) / DMI.Scale));
        }

        public string GetControlName()
        {
            int x = (int)((UserInput.MouseX - DrawPosition.X) / DMI.Scale);
            int y = (int)((UserInput.MouseY - DrawPosition.Y) / DMI.Scale);
            foreach (var area in DMI.ActiveWindow.SubAreas)
            {
                if (!(area is DMIButton)) continue;
                var b = (DMIButton)area;
                if (b.SensitiveArea(DMI.ActiveWindow.Position).Contains(x, y)) return b.DisplayName;
            }
            return "";
        }
        public string ControlLabel => GetControlName();

        public void SetTexture3D()
        {
            IsTexture3D = true;
            Control.Width = DMI.Width;
            Control.Height = DMI.Height;
            DMI.SizeTo(DMI.Width, DMI.Height);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            DMI.Draw(ControlView.SpriteBatch, new Point(DrawPosition.X, DrawPosition.Y));
            ControlView.SpriteBatch.End();
            ControlView.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, Shader);
        }
        public Rectangle DestinationRectangleGet()
        {
            return DrawPosition;
        }
        public bool isMouseControl()
        {
            return true;
        }
    }

    public class ThreeDimCabScreen// : ICabViewMouseControlRenderer
    {
        PoseableShape TrainCarShape;
        Matrix XNAMatrix;
        Viewer Viewer;
        public CabViewControlRenderer CVFR;
        public ThreeDimCabScreen(Viewer viewer, int iMatrix, PoseableShape trainCarShape, CabViewControlRenderer c)
        {
            CVFR = c;
            Viewer = viewer;
            TrainCarShape = trainCarShape;
            XNAMatrix = TrainCarShape.SharedShape.Matrices[iMatrix];
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!CVFR.IsPowered && CVFR.Control.HideIfDisabled)
                return;

            CVFR.PrepareFrame(frame, elapsedTime);
        }
    }
}
