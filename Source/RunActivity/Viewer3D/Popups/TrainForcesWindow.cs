// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

#region Design Notes
// The intent is to provide real-time train-handling feeback of the forces
// within the train, particularly for long, heavy freight trains. The
// Forces HUD, display or browser, is hard to read for long trains.
// An alternative, better in the long-term, might be an external window
// that provides both in-train and over time feedback, as seen on
// professional train simulators.
//
// Coupler Force (longitudinal):
//   Shows the length-wise pull or push force at each coupling, as a colored bar graph. Up
//   (positive) is pull, down (negative) is push. The scale is determined by the weakest
//   coupler in the train. The steps  are non-linear, to provide more sensitivity near the
//   breaking point.
//
// Derail Force (lateral):
//   Shows the sideway push or pull at the wheels as a colored bar graph. Up (positive) is
//   pull to the inside (stringline), down (negative) is push to the outside (jackknife).
//   The scale is determined by the lowest axle-load (lowest vertical force). The steps are
//   non-linear, to provide more sensitivity near the derailing point. But this is less
//   effective for lateral forces, as the force is proporation to the curve radius, which
//   changes in discrete steps.
//
// Brake Force:
//   Shows the brake force of each car as a bar graph. The scale is determined by the car
//   with the smallest brake force (smallest weight). The steps are non-linear, to provide
//   more sensitivity near the small brake applications. As the weight (and thus brake
//   force varies greatly between cars (and especially engines), the graph can be quite
//   jaggered.
//
// Bar Graph:
//   +/- 9 bars; 4 green, 3 orange, 2 red
//   blue middle-bar is an engine, white is a car
//   except: brake force only has the up side, and all bars are green.
//
// Slack:
//   Was considered, but is sufficiently reflected by the lateral force display.
//
// Grade & Curvature:
//   It is not practical to show grade or curvature in a meaningfule way
//   without needing significant screen-space and calculations. Thus they
//   are not included.
//
// Notes:
//   * Design was copied from the old (horizontal) train operations window. Using text-hight
//     for field-width, as text width is variable.
//   * All bar graphs present absolute forces (scaled by the weakest car in the train). This
//     provides a good overview of the actual forces along the train. An alternavive would be
//     to scale each bar for the the car it represents. That would be a better indication
//     where the risk is. But it would not reflect the real forces along the train.
//   * Lateral Forces and Derailment:
//     - As of Feb 2025, lateral forces are not calculated on straight track.
//     - As of Feb 2025, high longitudinal buff forces cause coupler breaks, not derailment.
#endregion

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using System;

namespace Orts.Viewer3D.Popups
{
    public class TrainForcesWindow : Window
    {
        private static Texture2D ForceBarTextures;
        private const int BarHight = 40;
        private const int HalfBarHight = 22;
        private const int BarWidth = 6;

        private Train PlayerTrain;
        private int LastPlayerTrainCars;
        private bool LastPlayerLocomotiveFlippedState;

        private static readonly float HighestRealisticCouplerStrengthN = 2.2e6f;  // 500k lbf, used for graph scale only
        private float LimitForCouplerStrengthN = HighestRealisticCouplerStrengthN;
        private float CouplerStrengthScaleN;

        private static readonly float HighestRealisticDerailForceN = 1.55e5f;  // 35k lbf, used for graph scale only
        private float LimitForDerailForceN = HighestRealisticDerailForceN;
        private float DerailForceScaleN;

        private static readonly float HighestRealisticBrakeForceN = 2.0e5f;  // 45k lbf, used for graph scale only
        private float BrakeForceScaleN;

        private Image[] CouplerForceBarGraph;
        private Image[] WheelForceBarGraph;
        private Image[] BrakeForceBarGraph;

        private Label MaxCouplerForceForTextBox;
        private Label MaxDerailForceForTextBox;

        // window size
        private readonly WindowTextFont Font;
        private readonly int TextHight;
        private readonly int GraphLabelWidth;
        private readonly int TextLineWidth;
        private readonly int WindowHeight;
        private readonly int WindowWidthMin;
        private readonly int WindowWidthMax;

        private bool IsMetric;

        /// <summary>
        /// Constructor. Initial window is wide enough for the two current forces in the
        /// line. This is good for about 50 cars. Window will resize when the number of
        /// cars is greater, and will scroll when the window would have to be greater
        /// than 1000 pixels.
        /// </summary>
        public TrainForcesWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + 500,
                   Window.DecorationSize.Y + owner.TextFontDefault.Height * 2 + BarHight * 2 + HalfBarHight + 18,
                   Viewer.Catalog.GetString("Train Forces"))
        {
            Font = owner.TextFontDefault;  // for Font.MeasureString(string)
            TextHight = Font.Height;
            GraphLabelWidth = TextHight * 6;
            TextLineWidth = TextHight * (9 + 7 + 9 + 7 + 8 + 5 + 8 + 5) + 2;
            WindowHeight = Location.Height;
            WindowWidthMin = Location.Width;
            WindowWidthMax = 1024 - Window.DecorationSize.X;
        }

        /// <summary>
        /// Initialize display. Loads static data, such as the bar graph images.
        /// </summary>
        protected internal override void Initialize()
        {
            base.Initialize();
            if (ForceBarTextures == null)
            {
                ForceBarTextures = SharedTextureManager.LoadInternal(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath,
                    "TrainForcesSprites.png"));
            }
        }

        /// <summary>
        /// Resize the window to fit the bar graph for the number of cars.
        /// Limited by min and max size.
        /// </summary>
        protected void ResizeWindow(int newWidth)
        {
            if (newWidth < WindowWidthMin) { newWidth = WindowWidthMin; }
            else if (newWidth > WindowWidthMax) { newWidth = WindowWidthMax; }

            SizeTo(newWidth, WindowHeight);
        }

        /// <summary>
        /// Create the layout. Defines the components within the window.
        /// </summary>
        protected override ControlLayout Layout(ControlLayout layout)
        {
            var innerBoxWidth = TextLineWidth;
            if (PlayerTrain != null && PlayerTrain.Cars != null)
            {
                int barGraphWidth = GraphLabelWidth + BarWidth * PlayerTrain.Cars.Count + 4;
                if (barGraphWidth > innerBoxWidth) { innerBoxWidth = barGraphWidth; }
            }

            var hbox = base.Layout(layout).AddLayoutHorizontal();
            var scrollbox = hbox.AddLayoutScrollboxHorizontal(hbox.RemainingHeight);
            var vbox = scrollbox.AddLayoutVertical(Math.Max(innerBoxWidth,scrollbox.RemainingWidth));
            var couplerForceBox = vbox.AddLayoutHorizontal(BarHight + 4);
            couplerForceBox.Add(new Label(0, (BarHight - TextHight) / 2, GraphLabelWidth, BarHight, Viewer.Catalog.GetString("Coupler") + ": "));
            var derailForceBox = vbox.AddLayoutHorizontal(BarHight + 4);
            derailForceBox.Add(new Label(0, (BarHight - TextHight) / 2, GraphLabelWidth, BarHight, Viewer.Catalog.GetString("Derail") + ": "));
            var brakeForceBox = vbox.AddLayoutHorizontal(HalfBarHight + 4);
            brakeForceBox.Add(new Label(0, (HalfBarHight - TextHight) / 2, GraphLabelWidth, HalfBarHight, Viewer.Catalog.GetString("Brake") + ": "));

            if (PlayerTrain != null)
            {
                if (PlayerTrain.LeadLocomotive != null) { IsMetric = PlayerTrain.LeadLocomotive.IsMetric; }
                SetConsistProperties(PlayerTrain);

                CouplerForceBarGraph = new Image[PlayerTrain.Cars.Count];
                WheelForceBarGraph = new Image[PlayerTrain.Cars.Count];
                BrakeForceBarGraph = new Image[PlayerTrain.Cars.Count];

                int carPosition = 0;
                foreach (var car in PlayerTrain.Cars)
                {
                    couplerForceBox.Add(CouplerForceBarGraph[carPosition] = new Image(BarWidth, BarHight));
                    CouplerForceBarGraph[carPosition].Texture = ForceBarTextures;
                    UpdateCouplerForceImage(car, carPosition);

                    derailForceBox.Add(WheelForceBarGraph[carPosition] = new Image(BarWidth, BarHight));
                    WheelForceBarGraph[carPosition].Texture = ForceBarTextures;
                    UpdateWheelForceImage(car, carPosition);

                    brakeForceBox.Add(BrakeForceBarGraph[carPosition] = new Image(BarWidth, HalfBarHight));
                    BrakeForceBarGraph[carPosition].Texture = ForceBarTextures;
                    UpdateBrakeForceImage(car, carPosition);

                    carPosition++;
                }

                vbox.AddHorizontalSeparator();
                var textLine = vbox.AddLayoutHorizontalLineOfText();

                textLine.Add(new Label(TextHight * 9, TextHight, Viewer.Catalog.GetString("Max Coupler") + ": ", LabelAlignment.Right));
                textLine.Add(MaxCouplerForceForTextBox = new Label(TextHight * 7, TextHight, FormatStrings.FormatLargeForce(0f, IsMetric), LabelAlignment.Right));
                textLine.Add(new Label(TextHight * 9, TextHight, Viewer.Catalog.GetString("Max Derail") + ": ", LabelAlignment.Right));
                textLine.Add(MaxDerailForceForTextBox = new Label(TextHight * 7, TextHight, FormatStrings.FormatLargeForce(0f, IsMetric), LabelAlignment.Right));

                textLine.Add(new Label(TextHight * 8, TextHight, Viewer.Catalog.GetString("Low Coupler") + ": ", LabelAlignment.Right));
                textLine.Add(new Label(TextHight * 5, TextHight, FormatStrings.FormatLargeForce(LimitForCouplerStrengthN, IsMetric), LabelAlignment.Right));
                textLine.Add(new Label(TextHight * 8, TextHight, Viewer.Catalog.GetString("Low Derail") + ": ", LabelAlignment.Right));
                textLine.Add(new Label(TextHight * 5, TextHight, FormatStrings.FormatLargeForce(LimitForDerailForceN, IsMetric), LabelAlignment.Right));

                // no text for brake force
            }

            return hbox;
        }

        /// <summary>
        /// Prepare frame for rendering. Update the data (graphs and values in text box).
        /// </summary>
        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                if (PlayerTrain != Owner.Viewer.PlayerTrain || Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars ||
                    (Owner.Viewer.PlayerLocomotive != null && LastPlayerLocomotiveFlippedState != Owner.Viewer.PlayerLocomotive.Flipped))
                {
                    PlayerTrain = Owner.Viewer.PlayerTrain;
                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    if (Owner.Viewer.PlayerLocomotive != null) LastPlayerLocomotiveFlippedState = Owner.Viewer.PlayerLocomotive.Flipped;
                    ResizeWindow(Window.DecorationSize.X + GraphLabelWidth + BarWidth * PlayerTrain.Cars.Count + 4);
                    Layout();
                }
            }

            if (PlayerTrain != null)
            {
                if (PlayerTrain.Cars.Count != CouplerForceBarGraph.Length)
                {
                    ResizeWindow(Window.DecorationSize.X + GraphLabelWidth + BarWidth * PlayerTrain.Cars.Count + 4);
                    Layout();
                }

                var absMaxCouplerForceN = 0.0f; var couplerForceSign = 1.0f; var maxCouplerForceCarNum = 0;
                var absMaxDerailForceN = 0.0f; var derailForceSign = 1.0f; var maxDerailForceCarNum = 0;

                int carPosition = 0;
                foreach (var car in PlayerTrain.Cars)
                {
                    UpdateCouplerForceImage(car, carPosition);
                    UpdateWheelForceImage(car, carPosition);
                    UpdateBrakeForceImage(car, carPosition);

                    var couplerForceN = car.CouplerForceU; var absCouplerForceN = Math.Abs(couplerForceN);
                    if (absCouplerForceN > absMaxCouplerForceN)
                    {
                        absMaxCouplerForceN = absCouplerForceN;
                        couplerForceSign = couplerForceN > 0 ? -1.0f : 1.0f;
                        maxCouplerForceCarNum = carPosition + 1;
                    }

                    // see TrainCar.UpdateTrainDerailmentRisk()
                    var absDerailForceN = car.TotalWagonLateralDerailForceN;
                    if (car.WagonNumBogies <= 0 || car.GetWagonNumAxles() <= 0) { absDerailForceN = car.DerailmentCoefficient * DerailForceScaleN; }
                    if (absDerailForceN > absMaxDerailForceN)
                    {
                        absMaxDerailForceN = absDerailForceN;
                        derailForceSign = (car.CouplerForceU > 0 && car.CouplerSlackM < 0) ? -1.0f : 1.0f;
                        maxDerailForceCarNum = carPosition + 1;
                    }

                    carPosition++;
                }

                if (MaxCouplerForceForTextBox != null)
                {
                    // TODO: smooth the downslope
                    MaxCouplerForceForTextBox.Text = FormatStrings.FormatLargeForce(absMaxCouplerForceN * couplerForceSign, IsMetric) +
                        string.Format(" ({0,3})", maxCouplerForceCarNum);
                }

                if (MaxDerailForceForTextBox != null)
                {
                    MaxDerailForceForTextBox.Text = FormatStrings.FormatLargeForce(absMaxDerailForceN * derailForceSign, IsMetric) +
                        string.Format(" ({0,3})", maxDerailForceCarNum);
                }
            }
        }

        /// <summary>
        /// Get static force values from consist, such as coupler strength and
        /// force that causes the wheel to derail.
        /// </summary>
        private void SetConsistProperties(Train theTrain)
        {
            float lowestCouplerBreakN = HighestRealisticCouplerStrengthN;
            float lowestDerailForceN = HighestRealisticDerailForceN;
            float lowestMaxBrakeForceN = HighestRealisticBrakeForceN;

            foreach (var car in theTrain.Cars)
            {
                if (car is MSTSWagon wag)
                {
                    var couplerBreakForceN = wag.GetCouplerBreak2N() > 1000f ? wag.GetCouplerBreak2N() : wag.GetCouplerBreak1N();
                    if (couplerBreakForceN > 1000f && couplerBreakForceN < lowestCouplerBreakN) { lowestCouplerBreakN = couplerBreakForceN; }

                    // simplified from TrainCar.UpdateTrainDerailmentRisk()
                    var numWheels = wag.GetWagonNumAxles() * 2;
                    if (numWheels <= 0) { numWheels = 4; }  // err towards higher vertical force
                    var wheelDerailForceN = wag.MassKG / numWheels * wag.GetGravitationalAccelerationMpS2();
                    if (wheelDerailForceN > 1000f && wheelDerailForceN < lowestDerailForceN) { lowestDerailForceN = wheelDerailForceN; }

                    var maxBrakeForceN = wag.MaxBrakeForceN;
                    if (maxBrakeForceN > 1000f && maxBrakeForceN < lowestMaxBrakeForceN) { lowestMaxBrakeForceN = maxBrakeForceN; }
                }
            }
            LimitForCouplerStrengthN = lowestCouplerBreakN;
            CouplerStrengthScaleN = lowestCouplerBreakN * 1.05f;

            LimitForDerailForceN = lowestDerailForceN;
            DerailForceScaleN = lowestDerailForceN * 1.1f;

            BrakeForceScaleN = lowestMaxBrakeForceN * 1.5f;
        }

        /// <summary>
        /// Update the coupler force (longitudinal) icon for a car. The image has 19 icons;
        /// index 0 is max push, 9 is neutral, 18 is max pull.
        /// </summary>
        private void UpdateCouplerForceImage(TrainCar car, int carPosition)
        {
            var idx = 9;  // neutral
            var absForceN = Math.Abs(car.SmoothedCouplerForceUN);

            if (absForceN > 1000f && CouplerStrengthScaleN > 1000f)  // exclude improbabl values
            {
                // power scale, to be sensitve at limit:  1k lbf, 28%, 46%, 59%, 70%, 80%, 87%, 94%, 100%
                var relForce = absForceN / CouplerStrengthScaleN;
                var expForce = (Math.Pow(6, relForce) - 1) * 1.5 + 1;
                idx = (int)Math.Floor(expForce);
                idx = (car.SmoothedCouplerForceUN > 0f) ? idx * -1 + 9 : idx + 9; // positive force is push
                if (idx < 0) { idx = 0; } else if (idx > 18) { idx = 18; }
                // TODO: for push force, may need to scale differently (how?); containers derail at 300 klbf
            }

            if (car.WagonType == TrainCar.WagonTypes.Engine) { CouplerForceBarGraph[carPosition].Source = new Rectangle(1 + idx * BarWidth, 0, BarWidth, BarHight); }
            else { CouplerForceBarGraph[carPosition].Source = new Rectangle(1 + idx * BarWidth, BarHight, BarWidth, BarHight); }
        }

        /// <summary>
        /// Update the wheel force (lateral) icon for a car. The image has 19 icons;
        /// index 0 is max push (outside), 9 is neutral, 18 is max pull (inside).
        /// </summary>
        private void UpdateWheelForceImage(TrainCar car, int carPosition)
        {
            var idx = 9;  // neutral

            var absForceN = car.TotalWagonLateralDerailForceN;

            // see TrainCar.UpdateTrainDerailmentRisk()
            if (car.WagonNumBogies <= 0 || car.GetWagonNumAxles() <= 0)
            {
                absForceN = car.DerailmentCoefficient * DerailForceScaleN;
                if (car.CouplerForceU > 0 && car.CouplerSlackM < 0) { absForceN /= 1.77f; }  // push to outside
                else { absForceN /= 1.34f; }  // pull to inside
            }

            // see TrainCar.UpdateTrainDerailmentRisk()
            float directionalScaleN = DerailForceScaleN;
            if (car.CouplerForceU > 0 && car.CouplerSlackM < 0) { directionalScaleN /= 1.77f;  }  // push to outside
            else if (car.CouplerForceU < 0 && car.CouplerSlackM > 0) { directionalScaleN /= 1.34f; }  // pull to inside

            if (absForceN > 1000f && DerailForceScaleN > 1000f)  // exclude improbable values
            {
                // flatter scale due to discrete curve radus: 1k lbf, 21%, 37%, 51%, 64%, 74%, 84%, 93%, 100%
                var relForce = absForceN / DerailForceScaleN;
                var expForce = (Math.Pow(3, relForce) - 1) * 4 + 1;
                idx = (int)Math.Floor(expForce);
                idx = (car.CouplerForceU > 0f && car.CouplerSlackM < 0) ? idx * -1 + 9 : idx + 9; // positive force is push
                if (idx < 0) { idx = 0; } else if (idx > 18) { idx = 18; }
            }

            if (car.WagonType == TrainCar.WagonTypes.Engine) { WheelForceBarGraph[carPosition].Source = new Rectangle(1 + idx * BarWidth, 0, BarWidth, BarHight); }
            else { WheelForceBarGraph[carPosition].Source = new Rectangle(1 + idx * BarWidth, BarHight, BarWidth, BarHight); }
        }

        /// <summary>
        /// Update the brake force icon for a car. The image has 10 icons;
        /// index 0 is neutral, 9 is max braking.
        /// </summary>
        private void UpdateBrakeForceImage(TrainCar car, int carPosition)
        {
            var idx = 0;  // neutral
            bool isDynamicBrakes = false;

            var absForceN = Math.Abs(car.BrakeForceN);  // using Math.Abs() for safety and consistency
            var absDynForceN = Math.Abs(car.DynamicBrakeForceN);
            if (absDynForceN > absForceN) { isDynamicBrakes = true; }
            absForceN += absDynForceN;

            if (absForceN > 1000f && BrakeForceScaleN > 1000f)  // exclude improbabl values
            {
                // log scale, to be sensitve at small application:  1k lbf, 7%, 14%, 22%, 30%, 39%, 51%, 68%, 100%
                var relForce = absForceN / BrakeForceScaleN;
                var logForce = (1 / (1 + Math.Pow(10, -1.5f * relForce)) - 0.5f) * 17.05f + 1f;
                idx = (int)Math.Floor(logForce);
                if (idx < 0) { idx = 0; } else if (idx > 9) { idx = 9; }
            }

            if (car.WagonType == TrainCar.WagonTypes.Engine)
            {
                if (isDynamicBrakes) { BrakeForceBarGraph[carPosition].Source = new Rectangle(1 + idx * BarWidth, BarHight * 2, BarWidth, HalfBarHight); }
                else { BrakeForceBarGraph[carPosition].Source = new Rectangle(1 + idx * BarWidth, BarHight * 2 + HalfBarHight, BarWidth, HalfBarHight); }
            }
            else { BrakeForceBarGraph[carPosition].Source = new Rectangle(1 + idx * BarWidth, BarHight * 2 + HalfBarHight * 2, BarWidth, HalfBarHight); }
        }
    }
}
