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
using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D
{
    public class CircularSpeedGauge
    {
        // These constants are from ETCS specification
        const int Width = 280;
        const int Height = 300;
        readonly float StartAngle = MathHelper.ToRadians(-144);
        readonly float EndAngle = MathHelper.ToRadians(144);
        readonly float MidAngle = MathHelper.ToRadians(48);
        const float MidSpeedKMpH = 200;
        const float MidSpeedMpH = 124.2f;
        const int RadiusOutside = 125;
        const int LineFull = 25;
        const int LineHalf = 15;
        const float FontHeightDial = 16;
        const float FontHeightReleaseSpeed = 17;
        const float FontHeightCurrentSpeed = 18;
        
        const int LineQuarter = 11;
        const int RadiusText = 99;
        readonly int[] CurrentSpeedPosition = new int[] { 150, 135, 120, 137 }; // x 10^0, x 10^1, x 10^2, y
        readonly Point ReleaseSpeedPosition = new Point(26 - 6, 274 - 8);
        readonly int[] UnitCenterPosition = new int[] { 140, 204 };
        // 240 and 260 are non-standard scales by ETA, but national railways often use one of these instead of 250
        readonly int[] StandardScalesKMpH = new int[] { 140, 180, 240, 250, 260, 400 };
        readonly int[] StandardScalesMpH = new int[] { 87, 111, 155, 248 };

        // Color RGB values are from ETCS specification
        readonly Color ColorGrey = new Color(195, 195, 195);
        readonly Color ColorMediumGrey = new Color(150, 150, 150);
        readonly Color ColorDarkGrey = new Color(85, 85, 85);
        readonly Color ColorYellow = new Color(223, 223, 0);
        readonly Color ColorOrange = new Color(234, 145, 0);
        readonly Color ColorRed = new Color(191, 0, 2);
        //readonly Color ColorDarkYellow = new Color(105, 105, 0);
        //readonly Color ColorBackground = new Color(3, 17, 34); // dark blue
        
        const string UnitMetricString = "km/h";
        const string UnitImperialString = "mph";
        
        // Some national railways specify the unit (km/h or mph) is to be shown on dial, in contrast to ETA.
        bool UnitVisible;
        bool UnitMetric;
        // Some national railways specify quarter lines at 5 km/h are to be visible on 240 and 260 km/h dials.
        bool DialQuarterLines;
        // Some national railways specify the scale lines and numbers above a certain limit not to be visible
        int MaxVisibleScale;
        int[] StandardScales;
        
        float MidSpeed;
        string Unit;

        Color GaugeColor;
        Color NeedleColor;
        Color SpeedColor;
        static Color[] NeedleTextureData;

        readonly DriverMachineInterfaceShader Shader;
        readonly CabShader cabShader;
        readonly Viewer Viewer;
        MSTSLocomotive Locomotive;

        Texture2D ColorTexture;
        Texture2D NeedleTexture;

        bool Active; // Trying to fix thread safety issue in SetRange() with this
        int MaxSpeed;
        int SourceMaxSpeed;
        int SpeedText;
        float CurrentSpeedAngle;
        readonly TextPrimitive ReleaseSpeed;
        readonly TextPrimitive[] CurrentSpeed;
        List<TextPrimitive> DialSpeeds;
        List<Vector4> DialLineCoords;
        Func<float, float> SpeedFromMpS;

        WindowTextFont FontDialSpeeds;
        WindowTextFont FontReleaseSpeed;
        WindowTextFont FontCurrentSpeed;

        readonly Rectangle SourceRectangle = new Rectangle(0, 0, Width, Height);
        public float Scale { get; private set; }
        
        public CircularSpeedGauge(int width, int height, int maxSpeed, bool unitMetric, bool unitVisible, bool dialQuarterLines, int maxVisibleScale,
            MSTSLocomotive locomotive, Viewer viewer, CabShader shader)
        {
            UnitVisible = unitVisible;
            SetUnit(unitMetric);
            
            DialQuarterLines = dialQuarterLines;
            MaxSpeed = maxSpeed;
            MaxVisibleScale = maxVisibleScale;
            Viewer = viewer;
            Locomotive = locomotive;
            cabShader = shader;
            
            SizeTo(width, height);
            SetRange(MaxSpeed);

            CurrentSpeed = new TextPrimitive[3];
            for (var i = 0; i < CurrentSpeed.Length; i++)
                CurrentSpeed[i] = new TextPrimitive(new Point(CurrentSpeedPosition[i], CurrentSpeedPosition[3]), Color.Black, "0", FontCurrentSpeed);

            ReleaseSpeed = new TextPrimitive(ReleaseSpeedPosition, ColorGrey, String.Empty, FontReleaseSpeed);

            Shader = new DriverMachineInterfaceShader(Viewer.GraphicsDevice);
            if (NeedleTextureData == null)
            {
                NeedleTextureData = new Color[128 * 16];

                // Needle texture is according to ETCS specification
                for (var v = 0; v < 128; v++)
                    for (var u = 0; u < 16; u++)
                        NeedleTextureData[u + 16 * v] = (
                            v <= 15 && 5 < u && u < 9
                            || 15 < v && v <= 23 && 5f - (float)(v - 15) / 8f * 3f < u && u < 9f + (float)(v - 15) / 8f * 3f
                            || 23 < v && v < 82 && 2 < u && u < 12
                        ) ? Color.White : Color.Transparent;
            }
        }
        
        /// <summary>
        /// Select the actual unit of measure for speed
        /// </summary>
        /// <param name="unitMetric">If true, set unit to km/h. If false, set to mph.</param>
        public void SetUnit(bool unitMetric)
        {
            UnitMetric = unitMetric;
            if (unitMetric)
            {
                SpeedFromMpS = MpS.ToKpH;
                MidSpeed = MidSpeedKMpH;
                Unit = UnitVisible ? UnitMetricString : "";
                StandardScales = StandardScalesKMpH;
            }
            else
            {
                SpeedFromMpS = MpS.ToMpH;
                MidSpeed = MidSpeedMpH;
                Unit = UnitImperialString;
                StandardScales = StandardScalesMpH;
            }
        }
        
        /// <summary>
        /// Set new font heights to match the actual scale.
        /// </summary>
        public void SetFont()
        {
            FontDialSpeeds = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightDial * Scale, System.Drawing.FontStyle.Bold);
            FontReleaseSpeed = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightReleaseSpeed * Scale, System.Drawing.FontStyle.Regular);
            FontCurrentSpeed = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightCurrentSpeed * Scale, System.Drawing.FontStyle.Bold);

            foreach (var text in DialSpeeds)
                text.Font = FontDialSpeeds;
            if (ReleaseSpeed != null)
                ReleaseSpeed.Font = FontReleaseSpeed;
            if (CurrentSpeed != null)
                foreach (var text in CurrentSpeed)
                    text.Font = FontCurrentSpeed;
        }

        /// <summary>
        /// Resize control to fit into a new rectangle, by keeping aspect ratio.
        /// </summary>
        /// <param name="width">New width of control.</param>
        /// <param name="height">New height of control.</param>
        public void SizeTo(int width, int height)
        {
            Scale = Math.Min((float)height / Height, (float)width / Width);
        }

        /// <summary>
        /// Recalculate dial lines and numbers positions to a new scale.
        /// </summary>
        /// <param name="maxSpeedMpS">Maximal speed to show in m/s, which will be recalculated to the actual unit: km/h or mph</param>
        public void SetRange(float maxSpeedMpS)
        {
            var maxSpeed = (int)SpeedFromMpS(maxSpeedMpS);
            SetRange(maxSpeed);
        }

        /// <summary>
        /// Recalculate dial lines and numbers positions to a new scale.
        /// </summary>
        /// <param name="maxSpeed">Maximal speed to show in actual measuring unit: km/h or mph</param>
        public void SetRange(int maxSpeed)
        {
            foreach (var speed in StandardScales)
                if (maxSpeed <= speed) { MaxSpeed = speed; break; }

            if (MaxSpeed == SourceMaxSpeed)
                return;
            SourceMaxSpeed = MaxSpeed;
            MaxVisibleScale = MaxVisibleScale <= 0 ? MaxSpeed : MaxVisibleScale;

            Active = false;

            DialLineCoords = new List<Vector4>();
            DialSpeeds = new List<TextPrimitive>();

            SetFont();

            var longLine = 0;
            var textHeight = (float)FontDialSpeeds.Height / Scale;

            for (var speed = 0; speed <= MaxSpeed && speed <= MaxVisibleScale; speed += 5)
            {
                var angle = Speed2Angle(speed);
                float x = 0, y = 0;
                GetXY(RadiusOutside, angle, ref x, ref y);

                if (speed % 10 == 0 || !UnitMetric && MaxSpeed < 130)
                {
                    if (longLine == 0)
                    {
                        DialLineCoords.Add(new Vector4(x, y, LineFull, angle + MathHelper.PiOver2));

                        if (MaxSpeed != StandardScales[StandardScales.Length - 1] || speed < MidSpeed 
                            || UnitMetric && speed % 100 == 0
                            || !UnitMetric && speed % 40 == 0)
                        {
                            var textWidth = FontDialSpeeds.MeasureString(speed.ToString()) / Scale;
                            GetXY(RadiusText, angle, ref x, ref y);
                            x -= textWidth / 2f * (1f + (float)Math.Sin(angle));
                            y -= textHeight / 2f * (1f - (float)Math.Cos(angle));
                            // Cheating for better outlook:
                            if (UnitMetric && 240 <= MaxSpeed && MaxSpeed <= 260)
                                switch (speed)
                                {
                                    case 100: x -= textWidth / 4f; break;
                                    case 120: x -= textWidth / 10f; y -= textHeight / 6f; break;
                                    case 140: x += textWidth / 6f; y -= textHeight / 6f; break;
                                }

                            DialSpeeds.Add(new TextPrimitive(new Point((int)x, (int)y), Color.White, speed.ToString(), FontDialSpeeds));
                        }
                    }
                    else
                        DialLineCoords.Add(new Vector4(x, y, LineHalf, angle + MathHelper.PiOver2));
                    
                    longLine++;
                    longLine %= MaxSpeed != StandardScales[StandardScales.Length - 1] ? 2 : UnitMetric ? 5 : (speed + 5 > MidSpeed) ? 4 : 2;
                }
                else if (UnitMetric && (MaxSpeed == 240 || MaxSpeed == 260))
                {
                    DialLineCoords.Add(new Vector4(x, y, LineQuarter, angle + MathHelper.PiOver2));
                }
            }
            
            if (Unit != "")
            {
                var unitPosition = new Point((int)(UnitCenterPosition[0] - FontDialSpeeds.MeasureString(Unit) / Scale / 2f), (int)(UnitCenterPosition[1] - textHeight / 2f));
                DialSpeeds.Add(new TextPrimitive(unitPosition, Color.White, Unit, FontDialSpeeds));
            }
            
            Active = true;
        }

        /// <summary>
        /// Translate speed value to rotation angle
        /// </summary>
        /// <param name="speed">Speed in km/h or mph</param>
        /// <returns>Rotation angle relative to up direction</returns>
        private float Speed2Angle(float speed)
        {
            float angle;
            if (MaxSpeed != StandardScales[StandardScales.Length - 1])
                angle = StartAngle + speed / MaxSpeed * (EndAngle - StartAngle);
            else if (speed <= MidSpeed)
                angle = StartAngle + speed / MidSpeed * (MidAngle - StartAngle);
            else
                angle = MidAngle + (speed - MidSpeed) / (MaxSpeed - MidSpeed) * (EndAngle - MidAngle);

            return MathHelper.Clamp(angle, StartAngle, EndAngle);
        }

        private void GetXY(float radius, float angle, ref float x, ref float y)
        {
            // Zero angle is up, x is right, y is down
            x = (float)(radius * Math.Sin(angle) + Width / 2);
            y = -(float)(radius * Math.Cos(angle) - Height / 2);
        }

        private void SetData(float currentSpeed, int permittedSpeed, int targetSpeed, int releaseSpeed, float interventionSpeed, ORTS.Scripting.Api.MonitoringStatus status)
        {
            if (interventionSpeed < permittedSpeed)
                interventionSpeed = permittedSpeed;
            if (currentSpeed > permittedSpeed && currentSpeed > interventionSpeed)
                interventionSpeed = currentSpeed - 1;

            switch (status)
            {
                case ORTS.Scripting.Api.MonitoringStatus.Normal:
                    GaugeColor = targetSpeed < permittedSpeed ? Color.White : ColorDarkGrey;
                    NeedleColor = ColorMediumGrey; SpeedColor = Color.Black; releaseSpeed = 0; interventionSpeed = permittedSpeed; break;
                case ORTS.Scripting.Api.MonitoringStatus.Indication: GaugeColor = NeedleColor = Color.White; SpeedColor = Color.Black; interventionSpeed = permittedSpeed; break;
                case ORTS.Scripting.Api.MonitoringStatus.Overspeed: GaugeColor = NeedleColor = ColorYellow; SpeedColor = Color.Black; interventionSpeed = permittedSpeed; break;
                case ORTS.Scripting.Api.MonitoringStatus.Warning: GaugeColor = ColorYellow; NeedleColor = ColorOrange; SpeedColor = Color.Black; break;
                case ORTS.Scripting.Api.MonitoringStatus.Intervention: GaugeColor = ColorYellow; NeedleColor = ColorRed; SpeedColor = Color.White; break;
            }

            CurrentSpeedAngle = Speed2Angle(currentSpeed);

            var shaderAngles = new Vector4(Speed2Angle(targetSpeed), Speed2Angle(permittedSpeed), Speed2Angle(interventionSpeed), Speed2Angle(releaseSpeed));
            Shader.SetData(shaderAngles, GaugeColor, NeedleColor);

            SpeedText = (int)(currentSpeed + (currentSpeed < 1f || currentSpeed < (float)SpeedText ? 0.99999f : 0.49999f));

            for (int i = 0, d = 1; i < CurrentSpeed.Length; i++, d *= 10)
            {
                CurrentSpeed[i].Color = SpeedColor;
                CurrentSpeed[i].Text = (SpeedText >= d || SpeedText == 0 && d == 1) ? (SpeedText / d % 10).ToString() : String.Empty;
            }

            ReleaseSpeed.Text = releaseSpeed > 0 ? releaseSpeed.ToString() : String.Empty;
        }

        public void PrepareFrame()
        {
            var tcs = Locomotive.TrainControlSystem;
            SetData(Math.Abs(SpeedFromMpS(Locomotive.SpeedMpS)), (int)SpeedFromMpS(tcs.CurrentSpeedLimitMpS), (int)SpeedFromMpS(tcs.NextSpeedLimitMpS), 0, SpeedFromMpS(tcs.InterventionSpeedLimitMpS), tcs.MonitoringStatus);
        }

        public void Draw(SpriteBatch spriteBatch, Point position)
        {
            if (ColorTexture == null)
            {
                ColorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                ColorTexture.SetData(new[] { Color.White });
            }
            if (NeedleTexture == null)
            {
                NeedleTexture = new Texture2D(spriteBatch.GraphicsDevice, 16, 128);
                NeedleTexture.SetData(NeedleTextureData);
            }

            if (!Active) return;
            
            int x = 0, y = 0;

            foreach (var lines in DialLineCoords)
            {
                x = position.X + (int)(lines.X * Scale);
                y = position.Y + (int)(lines.Y * Scale);
                var length = (int)(lines.Z * Scale);
                spriteBatch.Draw(ColorTexture, new Rectangle(x, y, length, 1), null, Color.White, lines.W, new Vector2(0, 0), SpriteEffects.None, 0);
            }

            // Monogame Spritebatch change Shaders procedure.
            // Following spriteBatch.Begin statements must reflect those for CabSpriteBatchMaterial in materials.cs

            // Apply DriverMachineInterface Shader

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, Shader);
            
            // Draw gauge needle centre and speed limit markings

            spriteBatch.Draw(ColorTexture, new Vector2(position.X, position.Y), SourceRectangle, Color.Transparent, 0, new Vector2(0, 0), Scale, SpriteEffects.None, 0);

            // Re-apply normal Cab lighting Shader

            spriteBatch.End();
            if (cabShader != null)
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, cabShader);
            else
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null ,null);

            spriteBatch.Draw(ColorTexture, new Vector2(position.X, position.Y), SourceRectangle, Color.Transparent, 0, new Vector2(0, 0), Scale, SpriteEffects.None, 0);
            
            // End of spritebatch change Shaders

            foreach (var text in DialSpeeds)
            {
                x = position.X + (int)(text.Position.X * Scale);
                y = position.Y + (int)(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }

            x = position.X + (int)(Width * Scale / 2f);
            y = position.Y + (int)(Height * Scale / 2f);
            spriteBatch.Draw(NeedleTexture, new Vector2(x, y), null, NeedleColor, CurrentSpeedAngle, new Vector2(8, 105), Scale, SpriteEffects.None, 0);

            foreach (var text in CurrentSpeed)
            {
                if (text.Text == String.Empty)
                    continue;
                x = position.X + (int)(text.Position.X * Scale);
                y = position.Y + (int)(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }

            if (ReleaseSpeed.Text != String.Empty)
            {
                x = position.X + (int)(ReleaseSpeed.Position.X * Scale);
                y = position.Y + (int)(ReleaseSpeed.Position.Y * Scale);
                ReleaseSpeed.Draw(spriteBatch, new Point(x, y));
            }
        }

        private class TextPrimitive
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
    }

    /// <summary>
    /// Wrapper class for CircularSpeedGauge, to display it as UI window control.
    /// </summary>
    public class CircularSpeedGaugeControl : Control
    {
        public readonly CircularSpeedGauge CircularSpeedGauge;

        public CircularSpeedGaugeControl(int width, int height, float maxSpeedMpS, WindowManager owner)
            : base(0, 0, width, height)
        {
            CircularSpeedGauge = new CircularSpeedGauge(
                width,
                height,
                owner.Viewer.MilepostUnitsMetric ? (int)MpS.ToKpH(maxSpeedMpS) : (int)MpS.ToMpH(maxSpeedMpS),
                owner.Viewer.MilepostUnitsMetric,
                true,
                (int)MpS.ToKpH(maxSpeedMpS) == 240 || (int)MpS.ToKpH(maxSpeedMpS) == 260,
                0,
                (MSTSLocomotive)owner.Viewer.PlayerLocomotive,
                owner.Viewer,
                null
            );
        }

        /// <summary>
        /// Resize control to fit into a new rectangle, by keeping aspect ratio.
        /// </summary>
        /// <param name="width">The new width of the control</param>
        /// <param name="height">The new height of the control</param>
        public void SizeTo(int width, int height)
        {
            Position.Width = width;
            Position.Height = height;
            CircularSpeedGauge.SizeTo(width, height);
        }

        /// <summary>
        /// Recalculate dial lines and numbers positions to a new speed scale.
        /// </summary>
        /// <param name="maxSpeedMpS">Maximal speed to show in m/s, which will be recalculated to the actual unit: km/h or mph</param>
        public void SetRange(float maxSpeedMpS)
        {
            CircularSpeedGauge.SetRange(maxSpeedMpS);
        }

        public void PrepareFrame()
        {
            CircularSpeedGauge.PrepareFrame();
        }
        
        internal override void Draw(SpriteBatch spriteBatch, Point position)
        {
            // Hack to adjust for track monitor
            position.X += 8;
            position.Y += 20;
            
            CircularSpeedGauge.Draw(spriteBatch, position);
        }
    }

    /// <summary>
    /// Wrapper class for CircularSpeedGauge, to render it as a 2D cabview control
    /// </summary>
    public class CabViewCircularSpeedGaugeRenderer : CabViewDigitalRenderer
    {
        public readonly CircularSpeedGauge CircularSpeedGauge;
        float PrevScale = 1;

        [CallOnThread("Loader")]
        public CabViewCircularSpeedGaugeRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCDigital control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            CircularSpeedGauge = new CircularSpeedGauge(
                (int)Control.Width,
                (int)Control.Height,
                (int)Control.MaxValue,
                Control.Units == CABViewControlUnits.KM_PER_HOUR,
                true,
                Control.MaxValue == 240 || Control.MaxValue == 260,
                (int)Control.MinValue,
                Locomotive,
                Viewer,
                shader
            );
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            base.PrepareFrame(frame, elapsedTime);
            CircularSpeedGauge.PrepareFrame();

            CircularSpeedGauge.SizeTo(DrawPosition.Width, DrawPosition.Height);
            if (Math.Abs(1f - PrevScale / CircularSpeedGauge.Scale) > 0.1f)
            {
                PrevScale = CircularSpeedGauge.Scale;
                CircularSpeedGauge.SetFont();
            }
        }

		public override void Draw(GraphicsDevice graphicsDevice)
        {
            CircularSpeedGauge.Draw(CabShaderControlView.SpriteBatch, new Point(DrawPosition.X, DrawPosition.Y));
        }
    }

}
