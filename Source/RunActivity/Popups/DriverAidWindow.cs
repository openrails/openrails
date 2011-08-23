// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;

namespace ORTS.Popups
{
    /// <summary>
    /// Responsible for displaying a speedometer showing current speed, as well as the 
    /// maximum speed the driver may go, as defined by the brake curve. Such a system is 
    /// a key ingredient in train control systems.
    /// </summary>
    public class DriverAidWindow : Window
    {
        DriverAid DriverAid;

        public DriverAidWindow(WindowManager owner)
            : base(owner, 150, 135, "Driver Aid")
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();

            DriverAid = new DriverAid(Owner.Viewer.RenderProcess.Content, vbox.RemainingWidth, 150, Owner.Viewer.MilepostUnitsMetric);

            vbox.Add(DriverAid);

            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            // update driver aid window - convert m/s to km/h, and take absolute so
            // speed is non-negative.
            float trainSpeed = Math.Abs(Owner.Viewer.PlayerTrain.SpeedMpS * 3.6f);


            // for now, use 120 = clear, 0 = anything else. 
            // TODO: get actual target speed of signal ahead. Currently, signals
            // clear automatically on their own so that, by itself, the driver aid 
            // isn't showing all that much.
            int targetSpeed = 0;
            if (Owner.Viewer.PlayerTrain.TMaspect == TrackMonitorSignalAspect.Clear)
            {
                targetSpeed = 120;
            }

            // temporary: this shows what it would look like if you had to stop
            // at every signal, demonstrating stuff needed to get things working
            // inside the driver aid window
            targetSpeed = 20;

            float deceleration = 0.3f;
            float brakeCurveSpeed = BrakeCurves.ComputeCurve(Owner.Viewer.PlayerTrain.SpeedMpS, Owner.Viewer.PlayerTrain.distanceToSignal, targetSpeed / 3.6f, deceleration) * 3.6f;

            DriverAid.UpdateSpeed(trainSpeed);
            DriverAid.UpdateTargetDistance(Owner.Viewer.PlayerTrain.distanceToSignal);
            DriverAid.UpdateTargetSpeed(targetSpeed);
            DriverAid.UpdateBrakeCurveSpeed(brakeCurveSpeed);
        }
    }

    public class DriverAid : Control
    {
        /// <summary>
        /// Defines different levels of "warning".
        /// </summary>
        private enum DisplayColors
        {
            None,
            Green,
            Yellow,
            Red,
            Mask
        }

        /// <summary>
        /// The speedometer texture.
        /// </summary>
        static Texture2D BaseTexture;

        /// <summary>
        /// The texture of the primary needle in front of the speedometer texture.
        /// </summary>
        static Texture2D NeedleTexture;

        /// <summary>
        /// The texure of the indicator that shows the current brake curve.
        /// </summary>
        static Texture2D BrakeCurveTexture;

        /// <summary>
        /// Font used to indicate the current target speed.
        /// </summary>
        static SpriteFont SpeedFont;

        /// <summary>
        /// Font used to indicate the the current speed of the train.
        /// </summary>
        static SpriteFont SpeedFontSmall;

        /// <summary>
        /// Contains the single-pixel solid color textures for drawing items other than the speedometer and curved bars.
        /// </summary>
        static Dictionary<DisplayColors, Texture2D> SolidTextures = new Dictionary<DisplayColors, Texture2D>();

        /// <summary>
        /// Angle of the needle (degrees) when showing "0".
        /// </summary>
        private float MinAngle = -120f;

        /// <summary>
        /// Angle of the needle (degrees) when showing MaxSpeed;
        /// </summary>
        private float MaxAngle = 120f;

        /// <summary>
        /// Maximum displayable speed, in kmh.
        /// </summary>
        private float MaxSpeed = 160;

        /// <summary>
        /// Defines the width of the needle, in pixels.
        /// </summary>
        private int NEEDLE_WIDTH = 28;

        /// <summary>
        /// Defines the height of the needle, in pixels.
        /// </summary>
        private int NEEDLE_HEIGHT = 50;

        /// <summary>
        /// Defines the width of the brake curve indicator bitmap, in pixels.
        /// </summary>
        private int INDICATOR_WIDTH = 10;

        /// <summary>
        /// Defines the height of the brake curve indicator bitmap, in pixels.
        /// </summary>
        private int INDICATOR_HEIGHT = 56;

        /// <summary>
        /// Defines the Y coordinate of the top most point of a gauge tick,
        /// if the gauge tick is vertical.
        /// </summary>
        private const float TICK_OUTER_Y = 3;

        /// <summary>
        /// Defines the Y coordinate of the bottom most point of a gauge tick,
        /// if the gauge tick is vertical.
        /// </summary>
        private const float TICK_INNER_Y = 7;

        /// <summary>
        /// Width of each gauge tick.
        /// </summary>
        private const float TICK_WIDTH = 2;

        /// <summary>
        /// Defines the Y coordinate of the center of a gauge label, if the label 
        /// is drawn vertically above the center of the gauge.
        /// </summary>
        private const float LABEL_Y = 5;

        /// <summary>
        /// Font size, in pixels of gauge labels.
        /// </summary>
        private const float LABEL_FONT_SIZE = 8f;

        /// <summary>
        /// Defines the size of the gauge, in pixels.
        /// </summary>
        private const int GAUGE_SIZE = 110;

        /// <summary>
        /// Defines how much space to the left of the gauge is reserved for the distance bar.
        /// </summary>
        private const int MARGIN_LEFT = 25;

        /// <summary>
        /// Defines the space, in pixels, between the bottom of the speed warning box,
        /// and the top of the distance box.
        /// </summary>
        private const int SPEED_WARN_BOTTOM_MARGIN = 5;

        /// <summary>
        /// Vertical position of the target speed box.
        /// </summary>
        private const int TARGETSPEED_Y = 80;

        /// <summary>
        /// Target speed box width.
        /// </summary>
        private const int TARGETSPEED_W = 40;

        /// <summary>
        /// Target speed box height.
        /// </summary>
        private const int TARGETSPEED_H = 16;

        private const int CURVEDBAR_INNER_RADIUS = 39;
        private const int CURVEDBAR_OUTER_RADIUS = 45;

        /// <summary>
        /// Size of each curved bar, in degrees.
        /// </summary>
        private const int CURVE_BAR_SEGMENT_SIZE = 45;

        /// <summary>
        /// Minumum brake warning box size.
        /// </summary>
        private const int MIN_BRAKE_WARN_BOX_SIZE = 3;

        /// <summary>
        /// Maximum brake warning box size.
        /// </summary>
        private const int MAX_BRAKE_WARN_BOX_SIZE = 9;

        /// <summary>
        /// These are the labels to show on the speedo.
        /// </summary>
        private float[] SpeedLabels = new float[] { 0, 20, 40, 60, 80, 100, 120, 140, 160 };

        /// <summary> 
        /// True when we should draw the gauge labels.
        /// </summary>
        private bool DrawGaugeLabels = false;

        /// <summary>
        /// Defines the area that a full target distance bar occupies.
        /// </summary>
        private readonly Rectangle TargetDistanceRect = new Rectangle(4, 25, 15, 75);

        /// <summary>
        /// Non-linearity table defining the distance bar behaviour
        /// </summary>
        private readonly List<Vector2> TargetDistanceKeyPoints = new List<Vector2>
        {
           new Vector2(0,0),
           new Vector2(1000, 0.5f),
           new Vector2(3000, 0.75f),
           new Vector2(5000, 1)
        };

        /// <summary>
        /// The current angle of the needle, in radians.
        /// </summary>
        private float CurrentSpeedAngle;

        /// <summary>
        /// Current speed of the train, in kmh.
        /// </summary>
        private float CurrentSpeed;

        /// <summary>
        /// The current angle of the brake curve indicator.
        /// </summary>
        private float CurrentBrakeCurveAngle = 0f;

        /// <summary>
        /// The current speed of the brake curve, in kmh.
        /// </summary>
        private float CurrentBrakeCurveSpeed = 0f;

        /// <summary>
        /// The current value of the target distance bar, in the range 0 -> 1.
        /// </summary>
        private float CurrentDistanceHeight = 0;

        /// <summary>
        /// Defines the centre of the Speed Warning Box(above the target distant rectangle).
        /// </summary>
        private System.Drawing.Point SpeedWarningBoxCentre
        {
            get
            {
                System.Drawing.Point returnValue = new System.Drawing.Point(
                (int)System.Math.Round(TargetDistanceRect.X + TargetDistanceRect.Width / 2f, 0),
                (int)System.Math.Round(TargetDistanceRect.Y - TargetDistanceRect.Width / 2f - SPEED_WARN_BOTTOM_MARGIN, 0));

                return returnValue;
            }
        }

        private int CurrentSpeedWarningBoxSize
        {
           get
           {
                int returnValue = MIN_BRAKE_WARN_BOX_SIZE;

                // true when the driver exceeding the brake curve: this means a red bar above the brake curve indicator
                bool driverExceedsBrakeCurve = false;

                // true when the driver is going faster than the upcoming speed reduction speed
                bool driverExceedsTargetSpeed = false;

                float CurrentTargetSpeedAngle = 0;
                float angleMeasureFromTarget = 0;

                ComputeDriverState(ref driverExceedsBrakeCurve, ref driverExceedsTargetSpeed, ref CurrentTargetSpeedAngle, ref angleMeasureFromTarget);

                if (driverExceedsTargetSpeed)
                {
                   // this is how *close* to the brake curve the driver is: if it's negative, he's going
                   // faster than the brake curve.
                   float proximityToBrakeCurve = CurrentBrakeCurveSpeed - CurrentSpeed;

                   float max = MAX_BRAKE_WARN_BOX_SIZE;
                   float min = MIN_BRAKE_WARN_BOX_SIZE;

                   float a = 10; // show maximum at 10kmh from brake curve or above
                   float b = 40; // show minimum at 40kmh from brake curve or below

                   if (proximityToBrakeCurve < a)
                   {
                        returnValue = MAX_BRAKE_WARN_BOX_SIZE;
                   }
                   else if (proximityToBrakeCurve > b)
                   {
                        returnValue = 0; // box is not visible
                   }
                   else
                   {
                        // simple linear relationship
                        float slope = (min - max) / (b - a);

                        float yIntercept = max - slope * a;

                        returnValue = (int)System.Math.Round(slope * proximityToBrakeCurve + yIntercept, 0);
                   }
                }
                else
                {
                   returnValue = 0; // box is not visible
                }
                return returnValue;
            }
        }

        /// <summary>
        /// Stores the current target speed.
        /// </summary>
        private float CurrentTargetSpeed;

        /// <summary>
        /// The string to display as the current target speed.
        /// </summary>
        private string CurrentTargetSpeedString = "120";

        /// <summary>
        /// The string to display as the current speed.
        /// </summary>
        private string CurrentSpeedString = string.Empty;

        private bool IsMetric;

        private readonly ContentManager Content;

        public DriverAid(ContentManager Content, int width, int height, Boolean isDisplayMetric)
           : base(0, 0, width, height)
        {
           this.Content = Content;
            // All calculations use KMpH but values are converted to MpH for non-metric displays.
           this.IsMetric = isDisplayMetric;
        }

        /// <summary>
        /// Updates the current speed, in kmh.
        /// </summary>
        internal void UpdateSpeed(float speed)
        {

            if (speed > MaxSpeed)
            {
                // speedometer cannot exceed maximum speed 
                speed = MaxSpeed;
            }
            CurrentSpeed = speed;

            int roundedSpeed;
            if (IsMetric) {
                  CurrentSpeedAngle = SpeedToAngle(speed);
                  roundedSpeed = (int)System.Math.Round(speed, 0);
            } else {
                  CurrentSpeedAngle = SpeedToAngle(MpH.FromKpH(speed));
                  roundedSpeed = (int)System.Math.Round(MpH.FromKpH(speed), 0);
            }
            CurrentSpeedString = roundedSpeed.ToString("G");
        }

        /// <summary>
        /// Updates the current brake speed, in kmh.
        /// </summary>
        internal void UpdateBrakeCurveSpeed(float speed)
        {
           if (speed > MaxSpeed)
           {
                // speedometer cannot exceed maximum speed 
                speed = MaxSpeed;
           }

           if (IsMetric) {
                 CurrentBrakeCurveAngle = SpeedToAngle(speed);
           } else {
               CurrentBrakeCurveAngle = SpeedToAngle(MpH.FromKpH(speed));
           }
           CurrentBrakeCurveSpeed = speed;
        }

        /// <summary>
        /// Updates the current target distance, in metres.
        /// </summary>
        /// <param name="distance"></param>
        internal void UpdateTargetDistance(float distance)
        {
           if (distance < 0)
           {
                // we don't expect to see negative values here, but for safety,
                // clip to zero
                distance = 0;
           }

           CurrentDistanceHeight = 1;

           for (int i = 0; i < TargetDistanceKeyPoints.Count - 1; i++)
           {

                float current = TargetDistanceKeyPoints[i].X;
                float next = TargetDistanceKeyPoints[i + 1].X;


                if (distance >= current && distance <= next)
                {
                   float value = (distance - TargetDistanceKeyPoints[i].X) / (TargetDistanceKeyPoints[i + 1].X - TargetDistanceKeyPoints[i].X);
                   CurrentDistanceHeight = MathHelper.Lerp(TargetDistanceKeyPoints[i].Y, TargetDistanceKeyPoints[i + 1].Y, value);
                   break;
                }
            }
        }

        internal void UpdateTargetSpeed(int targetSpeed)
        {
            CurrentTargetSpeed = targetSpeed;
            int roundedSpeed;
            if (IsMetric) {
                  roundedSpeed = targetSpeed;
            } else {
                  roundedSpeed = (int)System.Math.Round(MpH.FromKpH(targetSpeed), 0);
            }
            CurrentTargetSpeedString = roundedSpeed.ToString("G");
        }

        /// <summary>
        /// Given a speed in kmh, convert to gauge angle, in radians.
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        private float SpeedToAngle(float speed)
        {
           return MathHelper.ToRadians((MaxAngle - MinAngle) * speed / MaxSpeed + MinAngle);
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
           int X = offset.X + Position.X;
           int Y = offset.Y + Position.Y;

           System.Drawing.Size needleSize = new System.Drawing.Size(NEEDLE_WIDTH, NEEDLE_HEIGHT);

           System.Drawing.Size indicatorSize = new System.Drawing.Size(INDICATOR_WIDTH, INDICATOR_HEIGHT);

           // defines the point within the needle bitmap that is rotation point
           Vector2 needleCenter = new Vector2(needleSize.Width / 2f, needleSize.Height - needleSize.Width / 2f);

           if (BaseTexture == null)
           {
                BaseTexture = new Texture2D(spriteBatch.GraphicsDevice, GAUGE_SIZE, GAUGE_SIZE, 1, TextureUsage.None, SurfaceFormat.Color);

                BaseTexture.SetData(GenerateLabels(GAUGE_SIZE));
           }

           if (NeedleTexture == null)
           {
                NeedleTexture = new Texture2D(spriteBatch.GraphicsDevice, needleSize.Width, needleSize.Height, 1, TextureUsage.None, SurfaceFormat.Color);

                NeedleTexture.SetData(GenerateNeedle(needleSize, needleCenter));
           }

           if (BrakeCurveTexture == null)
           {
                BrakeCurveTexture = new Texture2D(spriteBatch.GraphicsDevice, indicatorSize.Width, indicatorSize.Height, 1, TextureUsage.None, SurfaceFormat.Color);

                BrakeCurveTexture.SetData(GenerateBrakeCurveTexture(indicatorSize));
           }

           CreateSolidTexture(spriteBatch, DisplayColors.Green, Color.Green);
           CreateSolidTexture(spriteBatch, DisplayColors.Yellow, Color.Yellow);
           CreateSolidTexture(spriteBatch, DisplayColors.Red, Color.Red);
           CreateSolidTexture(spriteBatch, DisplayColors.None, Color.White);

           if (SpeedFont == null)
           {
                SpeedFont = Content.Load<SpriteFont>("DriverAidSpeedFont");
           }

           if (SpeedFontSmall == null)
           {
                SpeedFontSmall = Content.Load<SpriteFont>("DriverAidSpeedFontSmall");
           }

           // draw base 
           spriteBatch.Draw(BaseTexture, new Rectangle(X + MARGIN_LEFT, Y, GAUGE_SIZE, GAUGE_SIZE), Color.White);

           Vector2 gaugeCenterPoint = new Vector2(GAUGE_SIZE / 2f, GAUGE_SIZE / 2f);

           DrawCurvedIndicators(spriteBatch, X, Y, gaugeCenterPoint);

           // draw needle
           spriteBatch.Draw(
                NeedleTexture, // thing to draw
                new Vector2(X + MARGIN_LEFT + gaugeCenterPoint.X, Y + gaugeCenterPoint.Y), // destination location
                new Rectangle(0, 0, needleSize.Width, needleSize.Height), // source rect
                Color.White,
                CurrentSpeedAngle, // rotation angle
                needleCenter,
                1f,                             // scale
                SpriteEffects.None,
                0);                             // layer depth

           // draw brake speed indicator 
           spriteBatch.Draw(
                BrakeCurveTexture, // thing to draw
                new Vector2(X + MARGIN_LEFT + gaugeCenterPoint.X, Y + gaugeCenterPoint.Y), // destination location
                new Rectangle(0, 0, indicatorSize.Width, indicatorSize.Height), // source rect
                Color.White,
                CurrentBrakeCurveAngle, // rotation angle
                new Vector2(indicatorSize.Width / 2f, indicatorSize.Height),
                1f,                             // scale
                SpriteEffects.None,
                0);                             // layer depth

           // draw central speed value on the needle
           Vector2 needleSpeed = SpeedFontSmall.MeasureString(CurrentSpeedString);
           spriteBatch.DrawString(SpeedFontSmall, CurrentSpeedString, new Vector2(X + MARGIN_LEFT + gaugeCenterPoint.X - needleSpeed.X / 2f, Y + gaugeCenterPoint.Y - needleSpeed.Y / 2f), Color.Black);

           Texture2D currentSolidTexture = GetStandardColorTexture();

           // draw bar
           int barH = (int)System.Math.Round(TargetDistanceRect.Height * CurrentDistanceHeight, 0);
           spriteBatch.Draw(currentSolidTexture, new Rectangle(X + TargetDistanceRect.X, Y + TargetDistanceRect.Bottom - barH, TargetDistanceRect.Width, barH), Color.White);


           // draw border around target bar (left side)
           spriteBatch.Draw(SolidTextures[DisplayColors.None], new Rectangle(X + TargetDistanceRect.X - 1, Y + TargetDistanceRect.Y, 1, TargetDistanceRect.Height), Color.White);

           // draw border around target bar (right side)
           spriteBatch.Draw(SolidTextures[DisplayColors.None], new Rectangle(X + TargetDistanceRect.Right, Y + TargetDistanceRect.Y, 1, TargetDistanceRect.Height), Color.White);

           // draw border around target bar (top)
           spriteBatch.Draw(SolidTextures[DisplayColors.None], new Rectangle(X + TargetDistanceRect.X - 1, Y + TargetDistanceRect.Y - 1, TargetDistanceRect.Width + 2, 1), Color.White);

           // draw border around target bar (bottom)
           spriteBatch.Draw(SolidTextures[DisplayColors.None], new Rectangle(X + TargetDistanceRect.X - 1, Y + TargetDistanceRect.Bottom, TargetDistanceRect.Width + 2, 1), Color.White);

           // draw speed warning box
           int boxSize = CurrentSpeedWarningBoxSize;
           spriteBatch.Draw(GetWarningBoxTexture(), new Rectangle(X + SpeedWarningBoxCentre.X - boxSize, Y + SpeedWarningBoxCentre.Y - boxSize, boxSize * 2, boxSize * 2), Color.White);

           Vector2 targetSpeedBoxSize = new Vector2(TARGETSPEED_W, TARGETSPEED_H);

           // this is the box the target speed is displayed in
           Rectangle targetSpeedBoxRect = new Rectangle(
                X + MARGIN_LEFT + (int)(GAUGE_SIZE / 2f - targetSpeedBoxSize.X / 2f),
                Y + TARGETSPEED_Y, (int)targetSpeedBoxSize.X, (int)targetSpeedBoxSize.Y);

            // draw target speed box
           spriteBatch.Draw(GetWarningBoxTexture(), targetSpeedBoxRect, Color.White);

           Vector2 sz = SpeedFont.MeasureString(CurrentTargetSpeedString);

           Vector2 textPos = new Vector2(
                targetSpeedBoxRect.X + targetSpeedBoxRect.Width / 2 - sz.X / 2,
                targetSpeedBoxRect.Y + targetSpeedBoxRect.Height / 2 - sz.Y / 2);

           // draw target speed label
           spriteBatch.DrawString(SpeedFont, CurrentTargetSpeedString, new Vector2(textPos.X + 1, textPos.Y + 1), Color.Black);
           spriteBatch.DrawString(SpeedFont, CurrentTargetSpeedString, textPos, Color.White);
        }

        private Texture2D CurvedBarTexture;

        private void DrawCurvedIndicators(SpriteBatch spriteBatch, int X, int Y, Vector2 gaugeCenterPoint)
        {

           if (CurvedBarTexture == null)
           {
                CurvedBarTexture = new Texture2D(spriteBatch.GraphicsDevice, GAUGE_SIZE, GAUGE_SIZE);
           }

           using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(GAUGE_SIZE, GAUGE_SIZE))
           using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
           {
                // true when the driver exceeding the brake curve: this means a red bar above the brake curve indicator
                bool driverExceedsBrakeCurve = false;

                // true when the driver is going faster than the upcoming speed reduction speed
                bool driverExceedsTargetSpeed = false;

                float CurrentTargetSpeedAngle = 0;
                float angleMeasureFromTarget = 0;

                ComputeDriverState(ref driverExceedsBrakeCurve, ref driverExceedsTargetSpeed, ref CurrentTargetSpeedAngle, ref angleMeasureFromTarget);

                if (driverExceedsTargetSpeed)
                {
                   g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                   System.Drawing.PointF center = new System.Drawing.PointF(GAUGE_SIZE / 2f, GAUGE_SIZE / 2f);

                   System.Drawing.RectangleF arcBoundsInner = System.Drawing.RectangleF.FromLTRB(
                        center.X - CURVEDBAR_INNER_RADIUS,
                        center.Y - CURVEDBAR_INNER_RADIUS,
                        center.X + CURVEDBAR_INNER_RADIUS,
                        center.Y + CURVEDBAR_INNER_RADIUS);

                   System.Drawing.RectangleF arcBoundsOuter = System.Drawing.RectangleF.FromLTRB(
                        center.X - CURVEDBAR_OUTER_RADIUS,
                        center.Y - CURVEDBAR_OUTER_RADIUS,
                        center.X + CURVEDBAR_OUTER_RADIUS,
                        center.Y + CURVEDBAR_OUTER_RADIUS);

                   // we know we need to draw a curved bar starting at the target speed, and increasing around the gauge
                   // until the brake curve indicator

                   System.Drawing.Color color = System.Drawing.Color.Yellow;

                   using (System.Drawing.SolidBrush b = new System.Drawing.SolidBrush(color))
                   using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                   {
                        path.StartFigure();

                        // inner arc
                        path.AddArc(arcBoundsInner, MathHelper.ToDegrees(CurrentBrakeCurveAngle) - 90, MathHelper.ToDegrees(-angleMeasureFromTarget));

                        // outer arc
                        path.AddArc(arcBoundsOuter, MathHelper.ToDegrees(CurrentTargetSpeedAngle) - 90, MathHelper.ToDegrees(angleMeasureFromTarget));

                        path.CloseFigure();

                        g.Clip = new System.Drawing.Region(path);

                        g.Clear(color);
                   }

                   if (driverExceedsBrakeCurve)
                   {
                        using (System.Drawing.SolidBrush b = new System.Drawing.SolidBrush(color))
                        using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                        {
                            color = System.Drawing.Color.Red;

                            float startAngle = CurrentBrakeCurveAngle;

                            path.StartFigure();

                            // inner arc
                            path.AddArc(arcBoundsInner, MathHelper.ToDegrees(CurrentBrakeCurveAngle) - 90, MathHelper.ToDegrees(CurrentSpeedAngle - CurrentBrakeCurveAngle));

                            // outer arc
                            path.AddArc(arcBoundsOuter, MathHelper.ToDegrees(CurrentSpeedAngle) - 90, -MathHelper.ToDegrees(CurrentSpeedAngle - CurrentBrakeCurveAngle));

                            path.CloseFigure();

                            g.Clip = new System.Drawing.Region(path);

                            g.Clear(color);
                        }
                    }

                   CurvedBarTexture.SetData(BitmapToBytes(bmp));

                   spriteBatch.Draw(
                        CurvedBarTexture, // thing to draw
                        new Vector2(X + MARGIN_LEFT + gaugeCenterPoint.X, Y + gaugeCenterPoint.Y), // destination location
                        new Rectangle(0, 0, GAUGE_SIZE, GAUGE_SIZE), // source rect
                        Color.White,
                        0, // rotation angle
                        gaugeCenterPoint,
                        1f,                             // scale
                        SpriteEffects.None,
                        0);
                    }
                }
            }

          private void ComputeDriverState(ref bool driverExceedsBrakeCurve, ref bool driverExceedsTargetSpeed, ref float CurrentTargetSpeedAngle, ref float angleMeasureFromTarget)
          {
           CurrentTargetSpeedAngle = SpeedToAngle(CurrentTargetSpeed);

           angleMeasureFromTarget = CurrentBrakeCurveAngle - CurrentTargetSpeedAngle;

           if (angleMeasureFromTarget > 0)
           {
                // train has a brake curve value LARGER than the target speed. This means the train is in an area where
                // there is an upcoming reduction in speed

                // now: see if the driver is going FASTER than the target speed
                if (CurrentSpeedAngle > CurrentTargetSpeedAngle)
                {
                   // yes - driver will have to reduce speed to meet the upcoming target speed

                   driverExceedsTargetSpeed = true;

                   if (CurrentSpeedAngle > CurrentBrakeCurveAngle)
                   {
                        driverExceedsBrakeCurve = true;
                   }
                }
            }
        }

        /// <summary>
        /// Returns the correct texture to use for the warning box, as
        /// it is sometimes a different color than the other "common" elements.
        /// </summary>
        /// <returns></returns>
        private Texture2D GetWarningBoxTexture()
        {
            Texture2D returnValue = null;

            // true when the driver exceeding the brake curve: this means a red bar above the brake curve indicator
            bool driverExceedsBrakeCurve = false;

            // true when the driver is going faster than the upcoming speed reduction speed
            bool driverExceedsTargetSpeed = false;

            float CurrentTargetSpeedAngle = 0;
            float angleMeasureFromTarget = 0;

            ComputeDriverState(ref driverExceedsBrakeCurve, ref driverExceedsTargetSpeed, ref CurrentTargetSpeedAngle, ref angleMeasureFromTarget);

            if (driverExceedsBrakeCurve)
            {
                returnValue = SolidTextures[DisplayColors.Red];
            }
            else if (driverExceedsTargetSpeed)
            {
                returnValue = SolidTextures[DisplayColors.Yellow];
            }
            else
            {
                returnValue = SolidTextures[DisplayColors.None];
            }
            return returnValue;
        }

        /// <summary>
        /// Returns the correct texture to use for many elements in the driver aid.
        /// The texture depends large on how fast the train is going relative to its brake curve.
        /// </summary>
        /// <returns></returns>
        private Texture2D GetStandardColorTexture()
        {
            Texture2D returnValue = null;

            returnValue = SolidTextures[DisplayColors.Green];

            return returnValue;
        }

        private void CreateSolidTexture(SpriteBatch spriteBatch, DisplayColors displayColor, Color color)
        {
           if (SolidTextures.ContainsKey(displayColor) == false)
           {
                SolidTextures.Add(displayColor, new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color));
                SolidTextures[displayColor].SetData(new Color[] { color });
           }
        }

        //private void CreateCurvedBar(SpriteBatch spriteBatch, DisplayColors displayColor, System.Drawing.Color color)
        //{
        //   if (CurvedBars.ContainsKey(displayColor) == false)
        //   {
        //        CurvedBars.Add(displayColor, new Texture2D(spriteBatch.GraphicsDevice, GAUGE_SIZE, GAUGE_SIZE, 1, TextureUsage.None, SurfaceFormat.Color));

        //        CurvedBars[displayColor].SetData(GenerateCurvedBar(GAUGE_SIZE, CURVEDBAR_INNER_RADIUS, CURVEDBAR_OUTER_RADIUS, color, displayColor == DisplayColors.Mask));
        //   }
        //}

        //private byte[] GenerateCurvedBar(int size, float innerRadius, float outerRadius, System.Drawing.Color color, bool mask)
        //{
        //   byte[] returnValue = null;

        //   using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(size, size))
        //   using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
        //   {
        //        // draw the base first
        //        DrawCurvedBar(g, size, innerRadius, outerRadius, color, mask);

        //        // and then convert to a byte[]
        //        returnValue = BitmapToBytes(bmp);
        //   }

        //   return returnValue;
        //}

        //private void DrawCurvedBar(System.Drawing.Graphics g, int size, float innerRadius, float outerRadius, System.Drawing.Color color, bool mask)
        //{

        //   using (System.Drawing.SolidBrush b = new System.Drawing.SolidBrush(color))
        //   {
        //        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;


        //        System.Drawing.PointF center = new System.Drawing.PointF(size / 2f, size / 2f);

        //        System.Drawing.RectangleF arcBoundsInner = System.Drawing.RectangleF.FromLTRB(
        //           center.X - innerRadius,
        //           center.Y - innerRadius,
        //           center.X + innerRadius,
        //           center.Y + innerRadius);

        //        System.Drawing.RectangleF arcBoundsOuter = System.Drawing.RectangleF.FromLTRB(
        //           center.X - outerRadius,
        //           center.Y - outerRadius,
        //           center.X + outerRadius,
        //           center.Y + outerRadius);


        //        System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

        //        path.StartFigure();


        //        // inner arc
        //        path.AddArc(arcBoundsInner,-90 +CURVE_BAR_SEGMENT_SIZE , -CURVE_BAR_SEGMENT_SIZE);

        //        // vertical line
        //        path.AddLine(new System.Drawing.PointF(center.X, center.Y - innerRadius), new System.Drawing.PointF(center.X, center.Y - outerRadius));

        //        // outer arc
        //        path.AddArc(arcBoundsOuter, -90, CURVE_BAR_SEGMENT_SIZE);

        //        path.CloseFigure();



        //        g.Clip = new System.Drawing.Region(path);

        //        g.Clear(color);

        //        //if (!mask)
        //        //{
        //        //   p.Width = arcThickness;
        //        //}
        //        //else
        //        //{
        //        //   p.Width = arcThickness + 2f;
        //        //}

        //        //p.StartCap = System.Drawing.Drawing2D.LineCap.Square;
        //        //p.EndCap = System.Drawing.Drawing2D.LineCap.Square;

        //        //g.Clip = new System.Drawing.Region(new System.Drawing.RectangleF(center.X, 0, size / 2f, size / 2f));

        //        //g.DrawArc(p, arcBounds, -90, CURVE_BAR_SEGMENT_SIZE);
        //   }
        //}

        private byte[] GenerateLabels(int size)
        {
            byte[] returnValue = null;

            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(size, size))
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
            {
                // draw the base first
                DrawBase(g, size);

                // and then convert to a byte[]
                returnValue = BitmapToBytes(bmp);
            }
            return returnValue;
        }

        private void DrawBase(System.Drawing.Graphics g, int size)
        {
           System.Drawing.PointF center = new System.Drawing.PointF(size / 2f, size / 2f);

           System.Drawing.PointF p1 = new System.Drawing.PointF(center.X, TICK_OUTER_Y);
           System.Drawing.PointF p2 = new System.Drawing.PointF(center.X, TICK_INNER_Y);

           // label draw point
           System.Drawing.PointF p3 = new System.Drawing.PointF(center.X, LABEL_Y);

           using (System.Drawing.Pen tickPen = new System.Drawing.Pen(System.Drawing.Color.White))
           using (System.Drawing.Font font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, LABEL_FONT_SIZE))
           {
                tickPen.Width = TICK_WIDTH;
                tickPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                tickPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                foreach (var i in SpeedLabels)
                {
                    float angle = SpeedToAngle(i);

                    System.Drawing.PointF p1prime = RotatePoint(p1, center, angle);
                    System.Drawing.PointF p2prime = RotatePoint(p2, center, angle);
                    System.Drawing.PointF p3prime = RotatePoint(p3, center, angle);

                    g.DrawLine(System.Drawing.Pens.White, p1prime, p2prime);

                    string label = i.ToString("G");

                    System.Drawing.SizeF labelSize = g.MeasureString(label, font); // inflate the label size a bit

                    System.Drawing.PointF labelDrawPoint = new System.Drawing.PointF(p3prime.X - labelSize.Width / 2f, p3prime.Y - labelSize.Height / 2f);

                    if (DrawGaugeLabels)
                    {
                        g.DrawString(label, font, System.Drawing.Brushes.White, labelDrawPoint);
                    }
                }
            }
        }

        private System.Drawing.PointF RotatePoint(System.Drawing.PointF p, System.Drawing.PointF center, float angle)
        {
           return new System.Drawing.PointF((float)((p.X - center.X) * System.Math.Cos(angle)) - (float)((p.Y - center.Y) * System.Math.Sin(angle)) + center.X,
                                            (float)((p.X - center.X) * System.Math.Sin(angle)) + (float)((p.Y - center.Y) * System.Math.Cos(angle)) + center.Y);
        }

        /// <summary>
        /// Generates a generic speedometer for use with a Texture2D.
        /// </summary>
        /// <param name="sz"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        private byte[] GenerateNeedle(System.Drawing.Size sz, Vector2 center)
        {
           byte[] returnValue = null;

           using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(sz.Width, sz.Height))
           using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
           {
                // draw the needle first
                DrawNeedle(g, sz, center);

                // and then convert to a byte[]
                returnValue = BitmapToBytes(bmp);
           }
           return returnValue;
        }

        /// <summary>
        /// Generates a triangular indicator, and formats the result
        /// for use on a Texture2D.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private byte[] GenerateBrakeCurveTexture(System.Drawing.Size size)
        {
           byte[] returnValue = null;

           using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(size.Width, size.Height))
           using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
           {
                // draw the indicator first
                DrawBrakeIndicator(g, size.Width, size.Height);

                // and then convert to a byte[]
                returnValue = BitmapToBytes(bmp);
           }
           return returnValue;
        }

        /// <summary>
        /// Generates a triangular indicator.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void DrawBrakeIndicator(System.Drawing.Graphics g, int width, int height)
        {
           // make an equilateral triangle
           var indicatorPoints = new System.Drawing.PointF[] 
           {
                new System.Drawing.PointF(0,0),
                new System.Drawing.PointF(width,0),
                new System.Drawing.PointF(width/2f,width * 0.86602f)
           };
           g.FillPolygon(System.Drawing.Brushes.Red, indicatorPoints);
        }

        /// <summary>
        /// Draws a generic speedometer needle.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="sz"></param>
        /// <param name="center"></param>
        private void DrawNeedle(System.Drawing.Graphics g, System.Drawing.Size sz, Vector2 center)
        {
           float thickWidth = sz.Width / 4f;
           float thickHeight = NEEDLE_HEIGHT * 0.5f;

           float thinWidth = sz.Width / 8f;
           float thinHeight = sz.Height * 0.6f;

           // draw circle at the center of the needle
           g.FillEllipse(System.Drawing.Brushes.White, new System.Drawing.RectangleF(center.X - (float)sz.Width / 2f, center.Y - (float)sz.Width / 2f, (float)sz.Width, (float)sz.Width));

           // draw thick needle part
           g.FillRectangle(System.Drawing.Brushes.White, new System.Drawing.RectangleF(sz.Width / 2f - thickWidth / 2f, sz.Height - thickHeight - sz.Width / 2f, thickWidth, thickHeight));

           // draw thin needle part
           g.FillRectangle(System.Drawing.Brushes.White, new System.Drawing.RectangleF(sz.Width / 2f - thinWidth / 2f, 0, thinWidth, thinHeight));
        }

        /// <summary>
        /// Given a bitmap, convert it to an array of bytes for use in a Texture2D.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private static byte[] BitmapToBytes(System.Drawing.Bitmap bitmap)
        {
           byte[] returnValue = null;

           // then convert the bitmap to an array of bytes, suitable for passing to Texture2D
           BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

           returnValue = new byte[data.Height * data.Stride];

           Marshal.Copy(data.Scan0, returnValue, 0, returnValue.Length);

           bitmap.UnlockBits(data);

           return returnValue;
        }
    }
}
