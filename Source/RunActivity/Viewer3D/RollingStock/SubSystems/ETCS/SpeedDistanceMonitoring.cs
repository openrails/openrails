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
using Orts.Viewer3D.Popups;
using ORTS.Common;
using ORTS.Scripting.Api.ETCS;
using static Orts.Viewer3D.RollingStock.Subsystems.ETCS.DriverMachineInterface;

namespace Orts.Viewer3D.RollingStock.Subsystems.ETCS
{
    // Compliant with ERA_ERTMS_015560 version 3.6.0 (ETCS DRIVER MACHINE INTERFACE)
    public class CircularSpeedGauge : DMIArea
    {
        // These constants are from ETCS specification
        readonly float NoGaugeAngle = MathHelper.ToRadians(-150); // Special angle when gauge must not be shown
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

        public CircularSpeedGauge(int maxSpeed, bool unitMetric, bool unitVisible, bool dialQuarterLines, int maxVisibleScale, DriverMachineInterface dmi) : base(dmi, 280, 300)
        {
            UnitVisible = unitVisible;
            SetUnit(unitMetric);

            DialQuarterLines = dialQuarterLines;
            MaxSpeed = maxSpeed;
            MaxVisibleScale = maxVisibleScale;

            SetRange(MaxSpeed);

            CurrentSpeed = new TextPrimitive[3];
            foreach (int i in Enumerable.Range(0, CurrentSpeed.Length))
                CurrentSpeed[i] = new TextPrimitive(new Point(CurrentSpeedPosition[i], CurrentSpeedPosition[3]), Color.Black, "0", FontCurrentSpeed);

            ReleaseSpeed = new TextPrimitive(ReleaseSpeedPosition, ColorGrey, String.Empty, FontReleaseSpeed);

            if (NeedleTextureData == null)
            {
                NeedleTextureData = new Color[128 * 16];

                // Needle texture is according to ETCS specification
                foreach (int v in Enumerable.Range(0, 128))
                    foreach (int u in Enumerable.Range(0, 16))
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
        public override void ScaleChanged()
        {
            SetFont();
        }
        void SetFont()
        {
            FontDialSpeeds = GetFont(FontHeightDial, true);
            FontReleaseSpeed = GetFont(FontHeightReleaseSpeed);
            FontCurrentSpeed = GetFont(FontHeightCurrentSpeed, true);

            foreach (var text in DialSpeeds)
                text.Font = FontDialSpeeds;
            if (ReleaseSpeed != null)
                ReleaseSpeed.Font = FontReleaseSpeed;
            if (CurrentSpeed != null)
                foreach (var text in CurrentSpeed)
                    text.Font = FontCurrentSpeed;
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

            ScaleChanged();

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

        private void SetData(ETCSStatus status)
        {
            if (!Active || !status.SpeedAreaShown) return;
            float currentSpeed = Math.Abs(SpeedFromMpS(DMI.Locomotive.SpeedMpS));
            int permittedSpeed = (int)SpeedFromMpS(status.AllowedSpeedMpS);
            int targetSpeed = status.TargetSpeedMpS < status.AllowedSpeedMpS ? (int)SpeedFromMpS(status.TargetSpeedMpS.Value) : permittedSpeed;
            int releaseSpeed = (int)SpeedFromMpS(status.ReleaseSpeedMpS ?? 0);
            float interventionSpeed = SpeedFromMpS(status.InterventionSpeedMpS);

            if (interventionSpeed < permittedSpeed && interventionSpeed < releaseSpeed)
                interventionSpeed = Math.Max(permittedSpeed, releaseSpeed);
            if (currentSpeed > permittedSpeed && currentSpeed > interventionSpeed)
                interventionSpeed = Math.Max(currentSpeed - 1, releaseSpeed);

            switch (status.CurrentMode)
            {
                case Mode.SB:
                case Mode.NL:
                case Mode.PT:
                    NeedleColor = ColorGrey;
                    break;
                case Mode.TR:
                    NeedleColor = ColorRed;
                    break;
                case Mode.SH:
                case Mode.RV:
                    if (currentSpeed <= permittedSpeed)
                        NeedleColor = ColorGrey;
                    else
                        NeedleColor = status.CurrentSupervisionStatus == SupervisionStatus.Intervention ? ColorRed : ColorOrange;
                    break;
                case Mode.SR:
                case Mode.UN:
                    if (currentSpeed > permittedSpeed)
                        NeedleColor = status.CurrentSupervisionStatus == SupervisionStatus.Intervention ? ColorRed : ColorOrange;
                    else if (status.CurrentMonitor == Monitor.TargetSpeed)
                        NeedleColor = currentSpeed < targetSpeed ? ColorGrey : ColorYellow;
                    else if (targetSpeed < permittedSpeed && currentSpeed >= targetSpeed)
                        NeedleColor = Color.White;
                    else
                        NeedleColor = ColorGrey;
                    break;
                case Mode.LS:
                    if (currentSpeed > permittedSpeed)
                        NeedleColor = status.CurrentSupervisionStatus == SupervisionStatus.Intervention ? ColorRed : ColorOrange;
                    else if (status.CurrentMonitor == Monitor.ReleaseSpeed)
                        NeedleColor = ColorYellow;
                    else
                        NeedleColor = ColorGrey;
                    break;
                case Mode.FS:
                case Mode.OS:
                    if (currentSpeed > permittedSpeed)
                        NeedleColor = status.CurrentSupervisionStatus == SupervisionStatus.Intervention ? ColorRed : ColorOrange;
                    else if (status.CurrentMonitor == Monitor.TargetSpeed || status.CurrentMonitor == Monitor.ReleaseSpeed)
                        NeedleColor = currentSpeed < targetSpeed ? ColorGrey : ColorYellow;
                    else if (targetSpeed < permittedSpeed && currentSpeed >= targetSpeed)
                        NeedleColor = Color.White;
                    else
                        NeedleColor = ColorGrey;
                    break;
                case Mode.SN:
                    // TODO: Allow direct management of colors from STM
                    break;
            }
            SpeedColor = NeedleColor == ColorRed ? Color.White : Color.Black;
            if (status.CurrentMode == Mode.FS)
            {
                GaugeColor = status.CurrentMonitor == Monitor.TargetSpeed || status.CurrentMonitor == Monitor.ReleaseSpeed ? ColorYellow : Color.White;
                if (status.CurrentSupervisionStatus != SupervisionStatus.Overspeed && status.CurrentSupervisionStatus != SupervisionStatus.Warning && status.CurrentSupervisionStatus != SupervisionStatus.Intervention)
                    interventionSpeed = Math.Max(releaseSpeed, permittedSpeed);

                var shaderAngles = new Vector4(Speed2Angle(targetSpeed), Speed2Angle(permittedSpeed), Speed2Angle(interventionSpeed), Speed2Angle(releaseSpeed));
                DMI.Shader.SetData(shaderAngles, GaugeColor, NeedleColor, status.CurrentSupervisionStatus == SupervisionStatus.Intervention ? ColorRed : ColorOrange);
            }
            else
            {
                // CSG not shown
                var shaderAngles = new Vector4(NoGaugeAngle, NoGaugeAngle, NoGaugeAngle, NoGaugeAngle);
                DMI.Shader.SetData(shaderAngles, GaugeColor, NeedleColor, GaugeColor);
            }

            CurrentSpeedAngle = Speed2Angle(currentSpeed);

            SpeedText = (int)(currentSpeed + (currentSpeed < 1f || currentSpeed < (float)SpeedText ? 0.99999f : 0.49999f));

            for (int i = 0, d = 1; i < CurrentSpeed.Length; i++, d *= 10)
            {
                CurrentSpeed[i].Color = SpeedColor;
                CurrentSpeed[i].Text = (SpeedText >= d || SpeedText == 0 && d == 1) ? (SpeedText / d % 10).ToString() : String.Empty;
            }

            ReleaseSpeed.Text = releaseSpeed > 0 ? releaseSpeed.ToString() : String.Empty;
        }

        public override void PrepareFrame(ETCSStatus status)
        {
            SetData(status);
        }

        public override void Draw(SpriteBatch spriteBatch, Point position)
        {
            if (NeedleTexture == null)
            {
                NeedleTexture = new Texture2D(spriteBatch.GraphicsDevice, 16, 128);
                NeedleTexture.SetData(NeedleTextureData);
            }

            if (!Active) return;
            base.Draw(spriteBatch, position);

            int x, y;

            foreach (var lines in DialLineCoords)
            {
                x = position.X + (int)Math.Round(lines.X * Scale);
                y = position.Y + (int)Math.Round(lines.Y * Scale);
                var length = (int)Math.Round(lines.Z * Scale);
                spriteBatch.Draw(ColorTexture, new Rectangle(x, y, length, 1), null, Color.White, lines.W, new Vector2(0, 0), SpriteEffects.None, 0);
            }

            // Apply circular speed gauge shader
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, DMI.Shader);

            // Draw gauge needle centre and speed limit markings

            spriteBatch.Draw(ColorTexture, new Vector2(position.X, position.Y), new Rectangle(0, 0, Width, Height), Color.Transparent, 0, new Vector2(0, 0), Scale, SpriteEffects.None, 0);

            // Re-apply DMI shader
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.Default, null, null);

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
    }
    public class TTIandLSSMArea : DMIArea
    {
        int TTIWidth;
        Color TTIColor;
        const int T_dispTTI = 14;
        public TTIandLSSMArea(DriverMachineInterface dmi) : base(dmi, 54, 54)
        {

        }
        public override void PrepareFrame(ETCSStatus status)
        {
            TTIWidth = 0;
            float? tti = null;
            if (status.TimeToIndicationS.HasValue)
            {
                TTIColor = Color.White;
                tti = status.TimeToIndicationS;
            }
            if (status.TimeToPermittedS.HasValue)
            {
                switch (status.CurrentSupervisionStatus)
                {
                    case SupervisionStatus.Intervention:
                        TTIColor = ColorRed;
                        break;
                    case SupervisionStatus.Warning:
                    case SupervisionStatus.Overspeed:
                        TTIColor = ColorOrange;
                        break;
                    default:
                        TTIColor = ColorYellow;
                        break;
                }
                tti = status.TimeToPermittedS;
            }
            if (tti.HasValue)
            {
                foreach (int n in Enumerable.Range(1, 10))
                {
                    if (T_dispTTI * (10 - n) / 10f <= tti && tti < T_dispTTI * (10 - (n - 1)) / 10f)
                    {
                        TTIWidth = 5 * n;
                        break;
                    }
                }
            }
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            if (TTIWidth > 0)
            {
                if (TTIColor == Color.White) DrawRectangle(spriteBatch, drawPosition, 0, 0, 54, 54, ColorDarkGrey);
                DrawRectangle(spriteBatch, drawPosition, (54 - TTIWidth) / 2, (54 - TTIWidth) / 2, TTIWidth, TTIWidth, TTIColor);
            }
        }
    }
    public class TargetDistance : DMIArea
    {
        readonly int[] DistanceLinePositionsY = { -1, 6, 13, 22, 32, 45, 59, 79, 105, 152, 185 };
        readonly int[] DistanceLinePositionsX = { 12, 16, 16, 16, 16, 12, 16, 16, 16, 16, 12 };
        bool DisplayDistanceText;
        bool DisplayDistanceBar;
        Vector4 DistanceBar;
        TextPrimitive TargetDistanceText;
        WindowTextFont TargetDistanceFont;
        readonly float FontHeightTargetDistance = 10;
        public TargetDistance(DriverMachineInterface dmi) : base(dmi, 54, 221)
        {
            ScaleChanged();
        }
        public override void Draw(SpriteBatch spriteBatch, Point position)
        {
            base.Draw(spriteBatch, position);
            if (DisplayDistanceBar)
            {
                DrawRectangle(spriteBatch, position, DistanceBar.X, DistanceBar.Y + 30, DistanceBar.Z, DistanceBar.W, ColorGrey);

                // Distance speed lines
                foreach (int i in Enumerable.Range(0, 11))
                    DrawIntRectangle(spriteBatch, position, DistanceLinePositionsX[i], DistanceLinePositionsY[i] + 30, 25 - DistanceLinePositionsX[i], (int)Math.Max(1, 1 / Scale), ColorGrey);
            }
            if (DisplayDistanceText)
            {
                int x = position.X + (int)Math.Round(TargetDistanceText.Position.X * Scale);
                int y = position.Y + (int)Math.Round((TargetDistanceText.Position.Y + 54) * Scale);
                TargetDistanceText.Draw(spriteBatch, new Point(x, y));
            }
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            DisplayDistanceBar = DisplayDistanceText = false;
            if (!status.TargetDistanceM.HasValue) return;
            if (status.CurrentMode == Mode.OS || status.CurrentMode == Mode.SR) return;

            float dist = status.TargetDistanceM.Value;

            var text = (((int)(dist / 10)) * 10).ToString();
            var fontSize = TargetDistanceFont.MeasureString(text) / Scale;
            TargetDistanceText = new TextPrimitive(new Point((int)(54 - fontSize), (int)(30 - FontHeightTargetDistance) / 2), ColorGrey, text, TargetDistanceFont);

            if (dist > 1000) dist = 1000;
            double h;
            if (dist < 100) h = dist / 100 * (185 - 152);
            else
            {
                h = 185 - 152;
                h += (Math.Log10(dist) - 2) * (152 + 1);
            }
            DistanceBar = new Vector4(29, 186 - (float)h, 10, (float)h);

            DisplayDistanceText = true;
            DisplayDistanceBar = status.CurrentMode != Mode.SR;
        }
        public override void ScaleChanged()
        {
            SetFont();
        }
        void SetFont()
        {
            TargetDistanceFont = GetFont(FontHeightTargetDistance);
        }
    }
}
