/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

/// Author: Charlie Salts (aka: CommanderMath)
/// 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Diagnostics;

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
         : base(owner, 350, 280, "DriverAid")
      {
         Align(AlignAt.End, AlignAt.End);
      }

      protected override ControlLayout Layout(ControlLayout layout)
      {
         var vbox = base.Layout(layout).AddLayoutVertical();

         DriverAid = new DriverAid(Owner.Viewer.RenderProcess.Content, vbox.RemainingWidth, 260);

         vbox.Add(DriverAid);

         return vbox;
      }

      public void Update(float speed)
      {
         DriverAid.UpdateSpeed(speed);
      }
   }



   public class DriverAid : Control
   {

      private enum DisplayColors
      {
         None,
         Green,
         Yellow,
         Red
      }

      static Texture2D BaseTexture;

      static Texture2D NeedleTexture;
      static Texture2D BrakeCurveTexture;

      static Texture2D SolidRedTexture;
      static Texture2D SolidYellowTexture;
      static Texture2D SolidGreenTexture;
      static Texture2D SolidWhiteTexture;

      static SpriteFont SpeedFont;

      static Dictionary<DisplayColors, Texture2D> CurvedBars = new Dictionary<DisplayColors, Texture2D>();

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
      /// The maximum distance to show in the target distance bar.
      /// </summary>
      private float MaxDistance = 4000;


      /// <summary>
      /// Defines the width of the needle, in pixels.
      /// </summary>
      private int NEEDLE_WIDTH = 25;

      /// <summary>
      /// Defines the height of the needle, in pixels.
      /// </summary>
      private int NEEDLE_HEIGHT = 100;


      /// <summary>
      /// Defines the width of the brake curve indicator bitmap, in pixels.
      /// </summary>
      private int INDICATOR_WIDTH = 15;

      /// <summary>
      /// Defines the height of the brake curve indicator bitmap, in pixels.
      /// </summary>
      private int INDICATOR_HEIGHT = 120;

      /// <summary>
      /// Defines the Y coordinate of the top most point of a gauge tick,
      /// if the gauge tick is vertical.
      /// </summary>
      private const float TICK_OUTER_Y = 50;


      /// <summary>
      /// Defines the Y coordinate of the bottom most point of a gauge tick,
      /// if the gauge tick is vertical.
      /// </summary>
      private const float TICK_INNER_Y = 60;


      /// <summary>
      /// Defines the Y coordinate of the center of a gauge label, if the label 
      /// is drawn vertically above the center of the gauge.
      /// </summary>
      private const float LABEL_Y = 15;


      /// <summary>
      /// Font size, in pixels of gauge labels.
      /// </summary>
      private const float LABEL_FONT_SIZE = 11f;


      /// <summary>
      /// Defines the size of the gauge, in pixels.
      /// </summary>
      private const int GAUGE_SIZE = 270;


      /// <summary>
      /// Defines how much space to th left of the gauge is reserved for the distance bar.
      /// </summary>
      private const int MARGIN_LEFT = 50;


      /// <summary>
      /// Defines the space, in pixels, between the bottom of the speed warning box,
      /// and the top of the distance box.
      /// </summary>
      private const int SPEED_WARN_BOTTOM_MARGIN = 15;


      /// <summary>
      /// Vertical position of the target speed box.
      /// </summary>
      private const int TARGETSPEED_Y = 210;


      /// <summary>
      /// Target speed box width.
      /// </summary>
      private const int TARGETSPEED_W = 100;


      /// <summary>
      /// Target speed box height.
      /// </summary>
      private const int TARGETSPEED_H = 30;


      private const int CURVEDBAR_INNER_RADIUS = 104;
      private const int CURVEDBAR_OUTER_RADIUS = 115;


      /// <summary>
      /// These are the labels to show on the speedo.
      /// </summary>
      private float[] SpeedLabels = new float[] { 0, 20, 40, 60, 80, 100, 120, 140, 160 };


      /// <summary>
      /// Defines the area that a full target distance bar occupies.
      /// </summary>
      private readonly Rectangle TargetDistanceRect = new Rectangle(15, 60, 30, 180);


      /// <summary>
      /// The current angle of the needle, in radians.
      /// </summary>
      private float CurrentSpeedAngle;


      /// <summary>
      /// The current angle of the brake curve indicator.
      /// </summary>
      private float CurrentBrakeCurveAngle = -1;


      /// <summary>
      /// The current value of the target distance bar, in the range 0 -> 1.
      /// </summary>
      private float CurrentDistanceHeight = 0.25f;


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
            int returnValue = 10;

            // TODO

            return returnValue;
         }
      }


      /// <summary>
      /// The string to display as the current target speed.
      /// </summary>
      private string CurrentTargetSpeedString = "120";



      private readonly ContentManager Content;

      public DriverAid(ContentManager Content, int width, int height)
         : base(0, 0, width, height)
      {
         this.Content = Content;
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

         CurrentSpeedAngle = SpeedToAngle(speed);
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

         CurrentBrakeCurveAngle = SpeedToAngle(speed);
      }




      /// <summary>
      /// Updates the current target distance, in metres.
      /// </summary>
      /// <param name="distance"></param>
      internal void UpdateTargetDistance(float distance)
      {
         // TODO: evaluate 'distance' and convert to a value between 0, 1, inclusive.
         // Store value in CurrentDistanceHeight.
      }


      internal void UpdateTargetSpeed(int targetSpeed)
      {
         CurrentTargetSpeedString = targetSpeed.ToString("G");
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

         if (SolidGreenTexture == null)
         {
            SolidGreenTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            SolidGreenTexture.SetData(new Color[] { Color.Green });
         }

         if (SolidYellowTexture == null)
         {
            SolidYellowTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            SolidYellowTexture.SetData(new Color[] { Color.Yellow });
         }

         if (SolidRedTexture == null)
         {
            SolidRedTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            SolidRedTexture.SetData(new Color[] { Color.Green });
         }

         if (SolidWhiteTexture == null)
         {
            SolidWhiteTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            SolidWhiteTexture.SetData(new Color[] { Color.White });
         }

         if (CurvedBars.ContainsKey(DisplayColors.Green) == false)
         {
            CurvedBars.Add(DisplayColors.Green, new Texture2D(spriteBatch.GraphicsDevice, GAUGE_SIZE, GAUGE_SIZE, 1, TextureUsage.None, SurfaceFormat.Color));
            CurvedBars[DisplayColors.Green].SetData(GenerateCurvedBar(GAUGE_SIZE, CURVEDBAR_INNER_RADIUS, CURVEDBAR_OUTER_RADIUS, System.Drawing.Color.Green));
         }


         if (SpeedFont == null)
         {
            SpeedFont = Content.Load<SpriteFont>("DriverAidSpeedFont");
         }


         // draw base 
         spriteBatch.Draw(BaseTexture, new Rectangle(X + MARGIN_LEFT, Y, GAUGE_SIZE, GAUGE_SIZE), Color.White);

         // draw needle
         spriteBatch.Draw(
            NeedleTexture, // thing to draw
            new Vector2(X + MARGIN_LEFT + GAUGE_SIZE / 2f, Y + GAUGE_SIZE / 2f), // destination location
            new Rectangle(0, 0, needleSize.Width, needleSize.Height), // source rect
            Color.White,
            CurrentSpeedAngle, // rotation angle
            needleCenter,
            1f,                       // scale
            SpriteEffects.None,
            0);                       // layer depth


         spriteBatch.Draw(
            CurvedBars[DisplayColors.Green], // thing to draw
            new Vector2(X + MARGIN_LEFT + GAUGE_SIZE / 2f, Y + GAUGE_SIZE / 2f), // destination location
            new Rectangle(0, 0, GAUGE_SIZE, GAUGE_SIZE), // source rect
            Color.White,
            SpeedToAngle(0), // rotation angle
            new Vector2(GAUGE_SIZE / 2f, GAUGE_SIZE / 2f),
            1f,                       // scale
            SpriteEffects.None,
            0);                       // layer depth



         // draw brake speed indicator 
         spriteBatch.Draw(
            BrakeCurveTexture, // thing to draw
            new Vector2(X + MARGIN_LEFT + GAUGE_SIZE / 2f, Y + GAUGE_SIZE / 2f), // destination location
            new Rectangle(0, 0, indicatorSize.Width, indicatorSize.Height), // source rect
            Color.White,
            CurrentBrakeCurveAngle, // rotation angle
            new Vector2(indicatorSize.Width / 2f, indicatorSize.Height),
            1f,                       // scale
            SpriteEffects.None,
            0);                       // layer depth




         int barH = (int)System.Math.Round(TargetDistanceRect.Height * CurrentDistanceHeight, 0);

         // draw bar
         spriteBatch.Draw(SolidGreenTexture, new Rectangle(X + TargetDistanceRect.X, Y + TargetDistanceRect.Bottom - barH, TargetDistanceRect.Width, barH), Color.White);




         // draw border around target bar (left side)
         spriteBatch.Draw(SolidWhiteTexture, new Rectangle(X + TargetDistanceRect.X - 1, Y + TargetDistanceRect.Y, 1, TargetDistanceRect.Height), Color.White);

         // draw border around target bar (right side)
         spriteBatch.Draw(SolidWhiteTexture, new Rectangle(X + TargetDistanceRect.Right, Y + TargetDistanceRect.Y, 1, TargetDistanceRect.Height), Color.White);

         // draw border around target bar (top)
         spriteBatch.Draw(SolidWhiteTexture, new Rectangle(X + TargetDistanceRect.X - 1, Y + TargetDistanceRect.Y - 1, TargetDistanceRect.Width + 2, 1), Color.White);

         // draw border around target bar (bottom)
         spriteBatch.Draw(SolidWhiteTexture, new Rectangle(X + TargetDistanceRect.X - 1, Y + TargetDistanceRect.Bottom, TargetDistanceRect.Width + 2, 1), Color.White);


         int boxSize = CurrentSpeedWarningBoxSize;

         // draw speed warning box
         spriteBatch.Draw(SolidGreenTexture, new Rectangle(X + SpeedWarningBoxCentre.X - boxSize, Y + SpeedWarningBoxCentre.Y - boxSize, boxSize * 2, boxSize * 2), Color.White);





         int verticalPosition = 210;
         Vector2 targetSpeedBoxSize = new Vector2(100, 30);

         // this is the box the target speed is displayed in
         Rectangle targetSpeedBoxRect = new Rectangle(
            X + MARGIN_LEFT + (int)(GAUGE_SIZE / 2f - targetSpeedBoxSize.X / 2f),
            Y + verticalPosition, (int)targetSpeedBoxSize.X, (int)targetSpeedBoxSize.Y);

         // draw target speed box
         spriteBatch.Draw(SolidGreenTexture, targetSpeedBoxRect, Color.White);


         Vector2 sz = SpeedFont.MeasureString(CurrentTargetSpeedString);

         Vector2 textPos = new Vector2(
            targetSpeedBoxRect.X + targetSpeedBoxRect.Width / 2 - sz.X / 2,
            targetSpeedBoxRect.Y + targetSpeedBoxRect.Height / 2 - sz.Y / 2);

         // draw target speed label
         spriteBatch.DrawString(SpeedFont, CurrentTargetSpeedString, new Vector2(textPos.X + 2, textPos.Y + 2), Color.Black);
         spriteBatch.DrawString(SpeedFont, CurrentTargetSpeedString, textPos, Color.White);





      }

      private byte[] GenerateCurvedBar(int size, float innerRadius, float outerRadius, System.Drawing.Color color)
      {
         byte[] returnValue = null;

         using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(size, size))
         using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
         {
            // draw the base first
            DrawCurvedBar(g, size, innerRadius, outerRadius, color);

            // and then convert to a byte[]
            returnValue = BitmapToBytes(bmp);
         }

         return returnValue;
      }

      private void DrawCurvedBar(System.Drawing.Graphics g, int size, float innerRadius, float outerRadius, System.Drawing.Color color)
      {
         float arcThickness = outerRadius - innerRadius;

         Debug.Assert(arcThickness >= 0, "Driver Aid: outerRadius must be larger than or equal to innerRadius.");

         if (arcThickness <= 0)
         {
            arcThickness = 0;
         }

         using (System.Drawing.Pen p = new System.Drawing.Pen(color))
         {
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            p.Width = arcThickness;

            p.StartCap = System.Drawing.Drawing2D.LineCap.Square;
            p.EndCap = System.Drawing.Drawing2D.LineCap.Square;

            System.Drawing.PointF center = new System.Drawing.PointF(size / 2f, size / 2f);


            float radius = innerRadius - arcThickness / 2f;

            System.Drawing.RectangleF arcBounds = System.Drawing.RectangleF.FromLTRB(
               center.X - radius,
               center.Y - radius,
               center.X + radius,
               center.Y + radius);

            g.Clip = new System.Drawing.Region(new System.Drawing.RectangleF(center.X,0,size/2f, size/2f));

            g.DrawArc(p, arcBounds, -90, 90);
         }
      }


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

         using (System.Drawing.Font font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, LABEL_FONT_SIZE))
         {
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


               //g.DrawEllipse(System.Drawing.Pens.Green, new System.Drawing.RectangleF(labelDrawPoint, new System.Drawing.SizeF(2, 2)));
               g.DrawString(label, font, System.Drawing.Brushes.White, labelDrawPoint);
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

         float thickWidth = sz.Width / 3f;
         float thickHeight = sz.Height - sz.Width / 2f - sz.Height / 3f;

         float thinWidth = sz.Width / 8f;
         float thinHeight = sz.Height / 2f;

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
