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
using Orts.Viewer3D.Popups;
using ORTS.Common;
using ORTS.Scripting.Api;
using ORTS.Scripting.Api.ETCS;
using static Orts.Viewer3D.RollingStock.Subsystems.ETCS.DriverMachineInterface;

namespace Orts.Viewer3D.RollingStock.Subsystems.ETCS
{
    // Compliant with ERA_ERTMS_015560 version 3.6.0 (ETCS DRIVER MACHINE INTERFACE)
    public class PlanningWindow
    {
        PlanningTarget? IndicationMarkerTarget;
        float? IndicationMarkerDistanceM;

        int MaxViewingDistanceM = 8000;
        const int MaxZoomDistanceM = 32000;
        const int MinZoomDistanceM = 1000;

        bool Visible;

        readonly Viewer Viewer;

        readonly DriverMachineInterface DMI;

        Texture2D ColorTexture;

        readonly Button ButtonScaleDown;
        readonly Button ButtonScaleUp;

        Texture2D SpeedReductionTexture;
        Texture2D YellowSpeedReductionTexture;
        Texture2D SpeedIncreaseTexture;

        Texture2D[] ScaleUpTexture = new Texture2D[2];
        Texture2D[] ScaleDownTexture = new Texture2D[2];

        readonly Dictionary<int, Texture2D> TrackConditionTextureData = new Dictionary<int, Texture2D>();

        List<Rectangle> PASPRectangles = new List<Rectangle>();
        Dictionary<Point, bool> GradientRectangles = new Dictionary<Point, bool>();

        struct LocatedTexture
        {
            public readonly Texture2D Texture;
            public readonly Point Position;
            public LocatedTexture(Texture2D texture, Point position)
            {
                Texture = texture;
                Position = position;
            }
            public LocatedTexture(Texture2D texture, int x, int y)
            {
                Texture = texture;
                Position = new Point(x, y);
            }
        }
        List<LocatedTexture> TrackConditionTextures = new List<LocatedTexture>();
        List<LocatedTexture> SpeedTargetTextures = new List<LocatedTexture>();

        List<TextPrimitive> DistanceScaleText = new List<TextPrimitive>();
        List<TextPrimitive> SpeedTargetText = new List<TextPrimitive>();
        List<TextPrimitive> GradientText = new List<TextPrimitive>();
        WindowTextFont FontDistance;
        WindowTextFont FontTargetSpeed;
        WindowTextFont FontGradient;
        const float FontHeightDistance = 10;
        const float FontHeightTargetSpeed = 10;
        const float FontHeightGradient = 10;

        readonly int[] LinePositions = { 283, 250, 206, 182, 164, 150, 107, 64, 21 };
        readonly int[] LineDistances = { 0, 25, 50, 75, 100, 125, 250, 500, 1000 };

        readonly int[] TrackConditionPositions = { 55, 80, 105 };

        /*readonly Point TrackConditionLocation = new Point(0, 15);
        readonly Point GradientLocation = new Point(115, 15);
        readonly Point PASPLocation = new Point(133, 15);*/

        public float Scale = 1;

        public PlanningWindow(DriverMachineInterface dmi, Viewer viewer, Point planningLocation)
        {
            Viewer = viewer;
            DMI = dmi;

            SpeedIncreaseTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols","Planning", "PL_21.png"));
            SpeedReductionTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Planning","PL_22.png"));
            YellowSpeedReductionTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Planning", "PL_23.png"));
            for (int i=1; i<37; i++)
            {
                if (i == 21) i = 24;
                Texture2D tex = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Planning", "PL_" + (i < 10 ? "0" : "") + i + ".png"));
                TrackConditionTextureData.Add(i, tex);
            }
            ScaleUpTexture[0] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_05.bmp"));
            ScaleUpTexture[1] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_03.bmp"));
            ScaleDownTexture[0] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_06.bmp"));
            ScaleDownTexture[1] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS", "symbols", "Navigation", "NA_04.bmp"));

            ButtonScaleUp = new Button("Scale Up", true, new Rectangle(planningLocation.X, planningLocation.Y + 285, 40, 30));
            ButtonScaleDown = new Button("Scale Down", true, new Rectangle(planningLocation.X, planningLocation.Y - 15, 40, 30));
            DMI.SensitiveButtons.Add(ButtonScaleUp);
            DMI.SensitiveButtons.Add(ButtonScaleDown);

            SetFont();
            SetDistanceText();
        }

        void ScaleUp()
        {
            if (MaxViewingDistanceM > MinZoomDistanceM)
            {
                if (MaxViewingDistanceM >= MaxZoomDistanceM) ButtonScaleDown.Enabled = true;
                MaxViewingDistanceM /= 2;
                SetDistanceText();
            }
            if (MaxViewingDistanceM <= MinZoomDistanceM) ButtonScaleUp.Enabled = false;
        }
        void ScaleDown()
        {
            if (MaxViewingDistanceM < MaxZoomDistanceM)
            {
                if (MaxViewingDistanceM <= MinZoomDistanceM) ButtonScaleUp.Enabled = true;
                MaxViewingDistanceM *= 2;
                SetDistanceText();
            }
            if (MaxViewingDistanceM >= MaxZoomDistanceM) ButtonScaleDown.Enabled = false;
        }
        Rectangle ScaledRectangle(Point origin, int x, int y, int width, int height)
        {
            return new Rectangle(origin.X + (int)(x * Scale), origin.Y + (int)(y * Scale), Math.Max((int)(width * Scale), 1), Math.Max((int)(height * Scale), 1));
        }
        public void Draw(SpriteBatch spriteBatch, Point position)
        {
            if (ColorTexture == null)
            {
                ColorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                ColorTexture.SetData(new[] { Color.White });
            }
            if (!Visible) return;

            // Planning area speed profile
            spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 14+133, 15, 99, 270), ColorPASPdark);
            foreach (Rectangle r in PASPRectangles)
            {
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, r.X + 133, r.Y + 15, r.Width, r.Height), ColorPASPlight);
            }

            // Distance lines
            for (int i = 0; i < 9; i++)
            {
                if (i == 0 || i == 5 || i == 8) spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 40, LinePositions[i], 200, 2), ColorMediumGrey);
                else spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 40, LinePositions[i], 200, 1), ColorDarkGrey);
            }

            // Indication marker
            if (IndicationMarkerDistanceM > 0) spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 14 + 133, GetPlanningHeight(IndicationMarkerDistanceM.Value), 93, 2), ColorYellow);

            // Speed target icons and numbers
            foreach (LocatedTexture lt in SpeedTargetTextures)
            {
                spriteBatch.Draw(lt.Texture, ScaledRectangle(position, lt.Position.X + 133, lt.Position.Y + 15, 20, 20), Color.White);
            }
            foreach (var text in SpeedTargetText)
            {
                text.Draw(spriteBatch, new Point(position.X + (int)((133 + text.Position.X) * Scale), position.Y + (int)((15 + text.Position.Y) * Scale)));
            }

            // Track condition icons
            foreach (LocatedTexture lt in TrackConditionTextures)
            {
                spriteBatch.Draw(lt.Texture, ScaledRectangle(position, lt.Position.X, lt.Position.Y + 15, 20, 20), Color.White);
            }

            // Distance scale digits
            foreach (var text in DistanceScaleText)
            {
                int x = position.X + (int)(text.Position.X * Scale);
                int y = position.Y + (int)(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }

            // Gradient profile
            foreach(var e in GradientRectangles)
            {
                int minp = e.Key.X + 15;
                int maxp = e.Key.Y + 15;
                int size = maxp - minp;
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 115, minp, 18, size), e.Value ? ColorGrey : ColorDarkGrey);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 115, minp, 18, 1), e.Value ? Color.White : ColorGrey);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 115, minp, 1, size), e.Value ? Color.White : ColorGrey);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 115, maxp - (int)Math.Max(1, 1/Scale), 18, 1), Color.Black);
            }
            foreach (var text in GradientText)
            {
                int x = position.X + (int)((115 + text.Position.X) * Scale);
                int y = position.Y + (int)((15 + text.Position.Y) * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }

            // Scale buttons
            spriteBatch.Draw(ScaleUpTexture[ButtonScaleUp.Enabled ? 1 : 0], ScaledRectangle(position, 14, 287, 12, 12), Color.White);
            spriteBatch.Draw(ScaleDownTexture[ButtonScaleDown.Enabled ? 1 : 0], ScaledRectangle(position, 14, 1, 12, 12), Color.White);
        }

        void CreateGradient(List<GradientProfileElement> GradientProfile)
        {
            var gradientText = new List<TextPrimitive>();
            var gradientRectangles = new Dictionary<Point, bool>();
            for (int i = 0; i + 1 < GradientProfile.Count; i++)
            {
                GradientProfileElement e = GradientProfile[i];
                if (e.DistanceToTrainM > MaxViewingDistanceM) break;

                float max = GradientProfile[i + 1].DistanceToTrainM;
                if (max < 0) continue;
                int minp = GetPlanningHeight(max) - 15;
                int maxp = GetPlanningHeight(e.DistanceToTrainM) - 15;
                if (max > MaxViewingDistanceM) minp = 0;
                int size = maxp - minp;
                gradientRectangles.Add(new Point(minp, maxp), e.GradientPerMille < 0);
                Color textColor = e.GradientPerMille < 0 ? Color.Black : Color.White;
                string sign = e.GradientPerMille < 0 ? "-" : "+";
                var signWidth = FontGradient.MeasureString(sign) / Scale;
                if (size > 44)
                {
                    string text = Math.Abs(e.GradientPerMille).ToString();
                    var fontWidth = FontGradient.MeasureString(text) / Scale;
                    gradientText.Add(new TextPrimitive(new Point((int)(9 - fontWidth / 2), (int)((minp + maxp - 1) / 2 - FontHeightGradient / 2)), textColor, text, FontGradient));
                    gradientText.Add(new TextPrimitive(new Point((int)(9 - signWidth / 2), minp + 3), textColor, sign, FontGradient));
                    gradientText.Add(new TextPrimitive(new Point((int)(9 - signWidth / 2), maxp - 8 - (int)FontHeightGradient), textColor, sign, FontGradient));
                }
                else if (size > 14)
                {
                    gradientText.Add(new TextPrimitive(new Point((int)(9 - signWidth / 2), (int)(((minp + maxp - 1) / 2 - FontHeightGradient / 2))), textColor, sign, FontGradient));
                }
            }
            GradientText = gradientText;
            GradientRectangles = gradientRectangles;
        }

        void CreatePASP(List<PlanningTarget> SpeedTargets)
        {
            List<Rectangle> paspRectangles = new List<Rectangle>();
            if (SpeedTargets.Count == 0) goto Exit;
            PlanningTarget prev_pasp = SpeedTargets[0];
            bool oth1 = false;
            bool oth2 = false;
            float widthFactor = 1;
            float allowedSpeedMpS = prev_pasp.TargetSpeedMpS;
            for (int i = 1; i < SpeedTargets.Count; i++)
            {
                PlanningTarget cur = SpeedTargets[i];
                PlanningTarget prev = SpeedTargets[i - 1];
                if (cur.DistanceToTrainM < 0) continue;
                if (cur.DistanceToTrainM > MaxViewingDistanceM)
                {
                    paspRectangles.Add(new Rectangle(14, 0, (int)(93 * widthFactor), GetPlanningHeight(prev_pasp.DistanceToTrainM) - 15 + (int)Math.Round(1 / Scale)));
                    break;
                }
                if (prev_pasp.TargetSpeedMpS > cur.TargetSpeedMpS && (!oth2 || cur.TargetSpeedMpS == 0))
                {
                    oth1 = true;
                    paspRectangles.Add(new Rectangle(14, GetPlanningHeight(cur.DistanceToTrainM) - 15, (int)(93 * widthFactor), GetPlanningHeight(prev_pasp.DistanceToTrainM) - GetPlanningHeight(cur.DistanceToTrainM) + (int)Math.Round(1 / Scale)));
                    float v = cur.TargetSpeedMpS / allowedSpeedMpS;
                    if (v > 0.74) widthFactor = 3.0f / 4;
                    else if (v > 0.49) widthFactor = 1.0f / 2;
                    else widthFactor = 1.0f / 4;
                    if (cur.TargetSpeedMpS == 0) break;
                    prev_pasp = cur;
                }
                if (oth1 && prev.TargetSpeedMpS < cur.TargetSpeedMpS) oth2 = true;
            }
        Exit:
            PASPRectangles = paspRectangles;
        }


        bool CheckTargetOverlap(PlanningTarget cur, PlanningTarget chk)
        {
            int a = GetPlanningHeight(cur.DistanceToTrainM);
            int b = GetPlanningHeight(chk.DistanceToTrainM);
            if (Math.Abs(a - b) > 18) return false;
            if (IndicationMarkerTarget.HasValue)
            {
                if (IndicationMarkerTarget.Value.Equals(chk)) return true;
                if (IndicationMarkerTarget.Value.Equals(cur)) return false;
            }
            return cur.TargetSpeedMpS > chk.TargetSpeedMpS;
        }

        void CreateTargetSpeeds(List<PlanningTarget> speedTargets)
        {
            var speedTargetText = new List<TextPrimitive>(speedTargets.Count);
            var speedTargetTextures = new List<LocatedTexture>(speedTargets.Count);
            int ld = 0;
            for (int i = 1; i < speedTargets.Count; i++)
            {
                bool overlap = false;
                for (int j = 1; j < speedTargets.Count; j++)
                {
                    if (i != j && CheckTargetOverlap(speedTargets[i], speedTargets[j]))
                    {
                        overlap = true;
                        break;
                    }
                }
                if (overlap) continue;
                PlanningTarget cur = speedTargets[i];
                PlanningTarget prev = speedTargets[ld];
                if (cur.DistanceToTrainM < 0) continue;
                ld = i;
                bool im = (IndicationMarkerDistanceM??-1) > 0 && IndicationMarkerTarget.Value.Equals(cur);
                if (cur.DistanceToTrainM > MaxViewingDistanceM) break;
                int a = GetPlanningHeight(cur.DistanceToTrainM) - 15;
                string text = ((int)MpS.ToKpH(cur.TargetSpeedMpS)).ToString();
                if (im || prev.TargetSpeedMpS > cur.TargetSpeedMpS || cur.TargetSpeedMpS == 0)
                {
                    speedTargetText.Add(new TextPrimitive(new Point(25, a - 2), im ? ColorYellow : ColorGrey, text, FontTargetSpeed));
                    speedTargetTextures.Add(new LocatedTexture(im ? YellowSpeedReductionTexture : SpeedReductionTexture, 4, a + 7 - 10));
                }
                else
                {
                    speedTargetText.Add(new TextPrimitive(new Point(25, a - 2 - (int)FontHeightTargetSpeed), ColorGrey, text, FontTargetSpeed));
                    speedTargetTextures.Add(new LocatedTexture(SpeedIncreaseTexture, 4, a - 7 - 10));
                }
                if (cur.TargetSpeedMpS == 0) break;
            }
            SpeedTargetText = speedTargetText;
            SpeedTargetTextures = speedTargetTextures;
        }

        void CreateTrackConditions(List<PlanningTrackCondition> trackConditions)
        {
            var trackConditionTextures = new List<LocatedTexture>(trackConditions.Count);
            int[] prevObject = { LinePositions[0] + 10, LinePositions[0] + 10, LinePositions[0] + 10 };
            for (int i = 0; i < trackConditions.Count; i++)
            {
                PlanningTrackCondition condition = trackConditions[i];
                int posy = GetPlanningHeight(condition.DistanceToTrainM) - 10;
                int row = i % 3;
                if (condition.DistanceToTrainM > MaxViewingDistanceM || condition.DistanceToTrainM < 0 || prevObject[row] - posy < 20) continue;
                prevObject[row] = posy;
                Texture2D tex;
                switch(condition.Type)
                {
                    case TrackConditionType.LowerPantograph:
                        tex = TrackConditionTextureData[condition.YellowColour ? 2 : 1];
                        break;
                    case TrackConditionType.RaisePantograph:
                        tex = TrackConditionTextureData[condition.YellowColour ? 4 : 3];
                        break;
                    case TrackConditionType.NeutralSectionAnnouncement:
                        tex = TrackConditionTextureData[condition.YellowColour ? 6 : 5];
                        break;
                    case TrackConditionType.EndOfNeutralSection:
                        tex = TrackConditionTextureData[condition.YellowColour ? 8 : 7];
                        break;
                    case TrackConditionType.NonStoppingArea:
                        tex = TrackConditionTextureData[9];
                        break;
                    case TrackConditionType.RadioHole:
                        tex = TrackConditionTextureData[10];
                        break;
                    case TrackConditionType.MagneticShoeInhibition:
                        tex = TrackConditionTextureData[condition.YellowColour ? 12 : 11];
                        break;
                    case TrackConditionType.EddyCurrentBrakeInhibition:
                        tex = TrackConditionTextureData[condition.YellowColour ? 14 : 13];
                        break;
                    case TrackConditionType.RegenerativeBrakeInhibition:
                        tex = TrackConditionTextureData[condition.YellowColour ? 16 : 15];
                        break;
                    case TrackConditionType.CloseAirIntake:
                        tex = TrackConditionTextureData[condition.YellowColour ? 19 : 17];
                        break;
                    case TrackConditionType.OpenAirIntake:
                        tex = TrackConditionTextureData[condition.YellowColour ? 20 : 18];
                        break;
                    case TrackConditionType.SoundHorn:
                        tex = TrackConditionTextureData[35];
                        break;
                    case TrackConditionType.TractionSystemChange:
                        switch(condition.TractionSystem)
                        {
                            case TractionSystem.NonFitted:
                                tex = TrackConditionTextureData[condition.YellowColour ? 24 : 23];
                                break;
                            case TractionSystem.AC25kV:
                                tex = TrackConditionTextureData[condition.YellowColour ? 26 : 25];
                                break;
                            case TractionSystem.AC15kV:
                                tex = TrackConditionTextureData[condition.YellowColour ? 28 : 27];
                                break;
                            case TractionSystem.DC3000V:
                                tex = TrackConditionTextureData[condition.YellowColour ? 30 : 29];
                                break;
                            case TractionSystem.DC1500V:
                                tex = TrackConditionTextureData[condition.YellowColour ? 32 : 31];
                                break;
                            case TractionSystem.DC750V:
                                tex = TrackConditionTextureData[condition.YellowColour ? 34 : 33];
                                break;
                            default:
                                continue;
                        }
                        break;
                    default:
                        continue;
                }
                trackConditionTextures.Add(new LocatedTexture(tex, TrackConditionPositions[row], posy));
            }
            TrackConditionTextures = trackConditionTextures;
        }

        public void PrepareFrame(ETCSStatus status)
        {
            if (Visible != status.PlanningAreaShown)
            {
                Visible = status.PlanningAreaShown;
                if (Visible)
                {
                    ButtonScaleUp.Enabled = MaxViewingDistanceM > MinZoomDistanceM;
                    ButtonScaleDown.Enabled = MaxViewingDistanceM < MaxZoomDistanceM;
                }
                else
                {
                    ButtonScaleDown.Enabled = false;
                    ButtonScaleUp.Enabled = false;
                }
            }
            if (Visible)
            {
                IndicationMarkerTarget = status.IndicationMarkerTarget;
                IndicationMarkerDistanceM = status.IndicationMarkerDistanceM;
                CreateTrackConditions(status.PlanningTrackConditions);
                CreatePASP(status.SpeedTargets);
                CreateTargetSpeeds(status.SpeedTargets);
                CreateGradient(status.GradientProfile);
            }
        }

        public void HandleInput()
        {
            if (DMI.PressedButton == ButtonScaleDown) ScaleDown();
            if (DMI.PressedButton == ButtonScaleUp) ScaleUp();
        }

        /// <summary>
        /// Set new font heights to match the actual scale.
        /// </summary>
        public void SetFont()
        {
            FontDistance = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightDistance * Scale, System.Drawing.FontStyle.Regular);
            FontTargetSpeed = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightTargetSpeed * Scale, System.Drawing.FontStyle.Regular);
            FontGradient = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightGradient * Scale, System.Drawing.FontStyle.Regular);

            foreach (var text in DistanceScaleText)
                text.Font = FontDistance;
        }

        /// <summary>
        /// Set the font text according to planning distance scale
        /// </summary>
        void SetDistanceText()
        {
            var distanceScaleText = new List<TextPrimitive>(DistanceScaleText.Count);
            for (int i = 0; i < 9; i++)
            {
                if (i == 0 || i > 4)
                {
                    string distance = (LineDistances[i] * MaxViewingDistanceM / 1000).ToString();
                    Point unitPosition = new Point((int)(40 - 2 - FontDistance.MeasureString(distance) / Scale), (int)(LinePositions[i] - FontHeightDistance / 2));
                    distanceScaleText.Add(new TextPrimitive(unitPosition, ColorMediumGrey, distance, FontDistance));
                }
            }
            DistanceScaleText = distanceScaleText;
        }

        private int GetPlanningHeight(float distanceM)
        {
            float firstLine = LineDistances[1] * MaxViewingDistanceM / 1000;
            if (distanceM < firstLine) return LinePositions[0] - (int)(((LinePositions[0] - LinePositions[1]) / firstLine) * distanceM);
            else return LinePositions[1] - (int)((LinePositions[1] - LinePositions[8]) / Math.Log10(MaxViewingDistanceM / firstLine) * Math.Log10(distanceM / firstLine));
        }
    }
}