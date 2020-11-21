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

namespace Orts.Viewer3D.RollingStock.Subsystems.ETCS
{
    // Compliant with ERA_ERTMS_015560 version 3.6.0 (ETCS DRIVER MACHINE INTERFACE)
    public class PlanningWindow
    {
        public List<PlanningTarget> SpeedTargets = new List<PlanningTarget>();
        public List<PlanningTrackCondition> TrackConditions = new List<PlanningTrackCondition>();
        PlanningTarget? IndicationMarkerTarget;
        float? IndicationMarkerDistanceM;
        public List<GradientProfileElement> GradientProfile = new List<GradientProfileElement>();

        int MaxViewingDistanceM = 8000;

        bool Visible = true;

        readonly Viewer Viewer;

        readonly Color ColorPASPlight = new Color(41, 74, 107);
        readonly Color ColorPASPdark = new Color(33, 49, 74);
        readonly Color ColorYellow = new Color(223, 223, 0);
        readonly Color ColorDarkGrey = new Color(85, 85, 85);
        readonly Color ColorMediumGrey = new Color(150, 150, 150);
        readonly Color ColorGrey = new Color(195, 195, 195);
        readonly Color ColorWhite = new Color(255, 255, 255);
        readonly Color ColorBlack = new Color(0, 0, 0);
        Texture2D ColorTexture;

        Texture2D SpeedReductionTexture;
        Texture2D YellowSpeedReductionTexture;
        Texture2D SpeedIncreaseTexture;

        Dictionary<int, Texture2D> TrackConditionTextures = new Dictionary<int, Texture2D>();

        List<TextPrimitive> DistanceText = new List<TextPrimitive>();
        WindowTextFont FontDistance;
        WindowTextFont FontTargetSpeed;
        WindowTextFont FontGradient;
        const float FontHeightDistance = 10;
        const float FontHeightTargetSpeed = 10;
        const float FontHeightGradient = 10;

        readonly int[] LinePositions = { 283, 250, 206, 182, 164, 150, 107, 64, 21 };
        readonly int[] LineDistances = { 0, 25, 50, 75, 100, 125, 250, 500, 1000 };

        readonly int[] TrackConditionPositions = { 55, 80, 105 };

        public float Scale = 1;

        public PlanningWindow(Viewer viewer)
        {
            Viewer = viewer;

            SpeedIncreaseTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS/symbols/Planning/PL_21.png"));
            SpeedReductionTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS/symbols/Planning/PL_22.png"));
            YellowSpeedReductionTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS/symbols/Planning/PL_23.png"));
            for (int i=1; i<37; i++)
            {
                if (i == 21) i = 24;
                Texture2D tex = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "ETCS/symbols/Planning/PL_" + (i < 10 ? "0" : "") + i + ".png"));
                TrackConditionTextures.Add(i, tex);
            }

            SetFont();
            SetDistanceText();
        }

        public void ZoomIn()
        {
            if (MaxViewingDistanceM > 1000) MaxViewingDistanceM /= 2;
            SetDistanceText();
        }
        public void ZoomOut()
        {
            if (MaxViewingDistanceM < 32000) MaxViewingDistanceM *= 2;
            SetDistanceText();
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
            DrawPASP(spriteBatch, new Point(position.X + (int)(133*Scale), position.Y + (int)(15*Scale)));

            DrawTargetSpeeds(spriteBatch, new Point(position.X + (int)(133 * Scale), position.Y + (int)(15 * Scale)));

            DrawTrackConditions(spriteBatch, new Point(position.X + (int)(0 * Scale), position.Y + (int)(15 * Scale)));

            for (int i = 0; i < 9; i++)
            {
                if (i == 0 || i == 5 || i == 8) spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 40, LinePositions[i], 200, 2), ColorMediumGrey);
                else spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 40, LinePositions[i], 200, 1), ColorDarkGrey);
            }
            foreach (var text in DistanceText)
            {
                int x = position.X + (int)(text.Position.X * Scale);
                int y = position.Y + (int)(text.Position.Y * Scale);
                text.Draw(spriteBatch, new Point(x, y));
            }

            DrawGradient(spriteBatch, new Point(position.X + (int)(115 * Scale), position.Y + (int)(15 * Scale)));
        }
        void DrawPASP(SpriteBatch spriteBatch, Point position)
        {
            spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 14, 0, 99, 270), ColorPASPdark);

            if (SpeedTargets.Count == 0) return;
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
                    spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 14, 0, (int)(93 * widthFactor), GetPlanningHeight(prev_pasp.DistanceToTrainM) - 15 + (int)Math.Round(1/Scale)), ColorPASPlight);
                    break;
                }
                if (prev_pasp.TargetSpeedMpS > cur.TargetSpeedMpS && (!oth2 || cur.TargetSpeedMpS == 0))
                {
                    oth1 = true;
                    spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 14, GetPlanningHeight(cur.DistanceToTrainM) - 15, (int)(93 * widthFactor), GetPlanningHeight(prev_pasp.DistanceToTrainM) - GetPlanningHeight(cur.DistanceToTrainM) + (int)Math.Round(1 / Scale)), ColorPASPlight);
                    float v = cur.TargetSpeedMpS / allowedSpeedMpS;
                    if (v > 0.74) widthFactor = 3.0f / 4;
                    else if (v > 0.49) widthFactor = 1.0f / 2;
                    else widthFactor = 1.0f / 4;
                    if (cur.TargetSpeedMpS == 0) break;
                    prev_pasp = cur;
                }
                if (oth1 && prev.TargetSpeedMpS < cur.TargetSpeedMpS) oth2 = true;
            }
            if (IndicationMarkerDistanceM > 0) spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 14, GetPlanningHeight(IndicationMarkerDistanceM.Value) - 15, 93, 2), ColorYellow);
        }


        bool CheckTargetOverlap(int i, int j)
        {
            PlanningTarget cur = SpeedTargets[i];
            PlanningTarget chk = SpeedTargets[j];
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

        void DrawTargetSpeeds(SpriteBatch spriteBatch, Point position)
        {
            int ld = 0;
            for (int i = 1; i < SpeedTargets.Count; i++)
            {
                bool overlap = false;
                for (int j = 1; j < SpeedTargets.Count; j++)
                {
                    if (i != j && CheckTargetOverlap(i, j))
                    {
                        overlap = true;
                        break;
                    }
                }
                if (overlap) continue;
                PlanningTarget cur = SpeedTargets[i];
                PlanningTarget prev = SpeedTargets[ld];
                if (cur.DistanceToTrainM < 0) continue;
                ld = i;
                bool im = (IndicationMarkerDistanceM??-1) > 0 && IndicationMarkerTarget.Value.Equals(cur);
                if (cur.DistanceToTrainM > MaxViewingDistanceM) break;
                int a = GetPlanningHeight(cur.DistanceToTrainM) - 15;
                string text = ((int)MpS.ToKpH(cur.TargetSpeedMpS)).ToString();
                if (im || prev.TargetSpeedMpS > cur.TargetSpeedMpS || cur.TargetSpeedMpS == 0)
                {
                    Point textPosition = new Point(position.X + (int)(25*Scale), position.Y + (int)((a - 2)*Scale));
                    FontTargetSpeed.Draw(spriteBatch, textPosition, text, im ? ColorYellow : ColorGrey);
                    spriteBatch.Draw(im ? YellowSpeedReductionTexture : SpeedReductionTexture, ScaledRectangle(position, 4, a + 7 - 10, 20, 20), Color.White);
                }
                else
                {
                    Point textPosition = new Point(position.X + (int)(25 * Scale), position.Y + (int)((a - 2 - FontHeightTargetSpeed) * Scale));
                    FontTargetSpeed.Draw(spriteBatch, textPosition, text, ColorGrey);
                    spriteBatch.Draw(SpeedIncreaseTexture, ScaledRectangle(position, 4, a - 7 - 10, 20, 20), Color.White);
                }
                if (cur.TargetSpeedMpS == 0) return;
            }
        }

        void DrawTrackConditions(SpriteBatch spriteBatch, Point position)
        {
            int[] prevObject = { LinePositions[0] + 10, LinePositions[0] + 10, LinePositions[0] + 10 };
            for (int i = 0; i < TrackConditions.Count; i++)
            {
                PlanningTrackCondition condition = TrackConditions[i];
                int posy = GetPlanningHeight(condition.DistanceToTrainM) - 10;
                int row = i % 3;
                if (condition.DistanceToTrainM > MaxViewingDistanceM || condition.DistanceToTrainM < 0 || prevObject[row] - posy < 20) continue;
                prevObject[row] = posy;
                Texture2D tex;
                switch(condition.Type)
                {
                    case TrackConditionType.LowerPantograph:
                        tex = TrackConditionTextures[condition.YellowColour ? 2 : 1];
                        break;
                    case TrackConditionType.RaisePantograph:
                        tex = TrackConditionTextures[condition.YellowColour ? 4 : 3];
                        break;
                    case TrackConditionType.NeutralSectionAnnouncement:
                        tex = TrackConditionTextures[condition.YellowColour ? 6 : 5];
                        break;
                    case TrackConditionType.EndOfNeutralSection:
                        tex = TrackConditionTextures[condition.YellowColour ? 8 : 7];
                        break;
                    case TrackConditionType.NonStoppingArea:
                        tex = TrackConditionTextures[9];
                        break;
                    case TrackConditionType.RadioHole:
                        tex = TrackConditionTextures[10];
                        break;
                    case TrackConditionType.MagneticShoeInhibition:
                        tex = TrackConditionTextures[condition.YellowColour ? 12 : 11];
                        break;
                    case TrackConditionType.EddyCurrentBrakeInhibition:
                        tex = TrackConditionTextures[condition.YellowColour ? 14 : 13];
                        break;
                    case TrackConditionType.RegenerativeBrakeInhibition:
                        tex = TrackConditionTextures[condition.YellowColour ? 16 : 15];
                        break;
                    case TrackConditionType.CloseAirIntake:
                        tex = TrackConditionTextures[condition.YellowColour ? 19 : 17];
                        break;
                    case TrackConditionType.OpenAirIntake:
                        tex = TrackConditionTextures[condition.YellowColour ? 20 : 18];
                        break;
                    case TrackConditionType.SoundHorn:
                        tex = TrackConditionTextures[24];
                        break;
                    case TrackConditionType.TractionSystemChange:
                        switch(condition.TractionSystem)
                        {
                            default:
                                continue; // TODO
                        }
                        break;
                    default:
                        continue;
                }
                spriteBatch.Draw(tex, ScaledRectangle(position, TrackConditionPositions[row], posy, 20, 20), Color.White);
            }
        }

        void DrawGradient(SpriteBatch spriteBatch, Point position)
        {
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
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 0, minp, 18, size), e.GradientPerMille < 0 ? ColorGrey : ColorDarkGrey);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 0, minp, 18, 1), e.GradientPerMille < 0 ? ColorWhite : ColorGrey);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 0, minp, 1, size), e.GradientPerMille < 0 ? ColorWhite : ColorGrey);
                spriteBatch.Draw(ColorTexture, ScaledRectangle(position, 0, maxp - 1, 18, 1), ColorBlack);
                if (size > 44)
                {
                    {
                        string text = Math.Abs(e.GradientPerMille).ToString();
                        var fontWidth = FontGradient.MeasureString(text) / Scale;
                        Point textPosition = new Point(position.X + (int)((9 - fontWidth/2) * Scale), position.Y + (int)(((minp + maxp - 1) / 2 - FontHeightGradient / 2) * Scale));
                        FontGradient.Draw(spriteBatch, textPosition, text, e.GradientPerMille < 0 ? ColorBlack : ColorWhite);
                    }
                    {
                        string text = e.GradientPerMille < 0 ? "-" : "+";
                        var fontWidth = FontGradient.MeasureString(text) / Scale;
                        Point textPosition = new Point(position.X + (int)((9 - fontWidth / 2) * Scale), position.Y + (int)((minp + 3) * Scale));
                        FontGradient.Draw(spriteBatch, textPosition, text, e.GradientPerMille < 0 ? ColorBlack : ColorWhite);
                    }
                    {
                        string text = e.GradientPerMille < 0 ? "-" : "+";
                        var fontWidth = FontGradient.MeasureString(text) / Scale;
                        Point textPosition = new Point(position.X + (int)((9 - fontWidth / 2) * Scale), position.Y + (int)((maxp - 8 - FontHeightGradient) * Scale));
                        FontGradient.Draw(spriteBatch, textPosition, text, e.GradientPerMille < 0 ? ColorBlack : ColorWhite);
                    }
                }
                else if (size > 14)
                {
                    string text = e.GradientPerMille < 0 ? "-" : "+";
                    var fontWidth = FontGradient.MeasureString(text) / Scale;
                    Point textPosition = new Point(position.X + (int)((9 - fontWidth / 2) * Scale), position.Y + (int)(((minp + maxp - 1) / 2 - FontHeightGradient / 2) * Scale));
                    FontGradient.Draw(spriteBatch, textPosition, text, e.GradientPerMille < 0 ? ColorBlack : ColorWhite);
                }
            }
        }

        public void PrepareFrame(ETCSStatus status)
        {
            /*SpeedTargets.Clear();
            SpeedTargets.Add(new PlanningTarget(0, 90));
            SpeedTargets.Add(new PlanningTarget(500, 120));
            SpeedTargets.Add(new PlanningTarget(2000, 60));
            SpeedTargets.Add(new PlanningTarget(6000, 0));
            IndicationMarkerTarget = SpeedTargets[1];
            IndicationMarkerDistanceM = 300;

            GradientProfile.Add(new GradientProfileElement(0, 5));
            GradientProfile.Add(new GradientProfileElement(500, -5));
            GradientProfile.Add(new GradientProfileElement(2000, 7));
            GradientProfile.Add(new GradientProfileElement(3000, -10));
            GradientProfile.Add(new GradientProfileElement(4000, 0));*/
            Visible = status.PlanningAreaShown;
            if (Visible)
            {
                SpeedTargets = status.SpeedTargets;
                IndicationMarkerTarget = status.IndicationMarkerTarget;
                IndicationMarkerDistanceM = status.IndicationMarkerDistanceM;
                GradientProfile = status.GradientProfile;
                TrackConditions = status.PlanningTrackConditions;
            }
        }

        /// <summary>
        /// Set new font heights to match the actual scale.
        /// </summary>
        public void SetFont()
        {
            FontDistance = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightDistance * Scale, System.Drawing.FontStyle.Regular);
            FontTargetSpeed = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightTargetSpeed * Scale, System.Drawing.FontStyle.Regular);
            FontGradient = Viewer.WindowManager.TextManager.GetExact("Arial", FontHeightGradient * Scale, System.Drawing.FontStyle.Regular);

            foreach (var text in DistanceText)
                text.Font = FontDistance;
        }

        /// <summary>
        /// Set the font text according to planning distance scale
        /// </summary>
        void SetDistanceText()
        {
            DistanceText.Clear();
            var textHeight = FontDistance.Height / Scale;
            for (int i = 0; i < 9; i++)
            {
                if (i == 0 || i > 4)
                {
                    string distance = (LineDistances[i] * MaxViewingDistanceM / 1000).ToString();
                    Point unitPosition = new Point((int)(40 - 2 - FontDistance.MeasureString(distance) / Scale), (int)(LinePositions[i] - textHeight / 2f));
                    DistanceText.Add(new TextPrimitive(unitPosition, ColorMediumGrey, distance, FontDistance));
                }
            }
        }

        private int GetPlanningHeight(float distanceM)
        {
            float firstLine = LineDistances[1] * MaxViewingDistanceM / 1000;
            if (distanceM < firstLine) return LinePositions[0] - (int)(((LinePositions[0] - LinePositions[1]) / firstLine) * distanceM);
            else return LinePositions[1] - (int)((LinePositions[1] - LinePositions[8]) / Math.Log10(MaxViewingDistanceM / firstLine) * Math.Log10(distanceM / firstLine));
        }
    }
}