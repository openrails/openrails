// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
{
    public enum TrackMonitorSignalAspect
    {
        None,
        Clear_2,
        Clear_1,
        Approach_3,
        Approach_2,
        Approach_1,
        Restricted,
        StopAndProceed,
        Stop,
        Permission,
    }

    public class TrackMonitorWindow : Window
    {
        public int MAXDISTANCE = 5000;
        public const int TrackMonitorHeight = 265;
        public const int LabelsHeight = 117;

        Label SpeedCurrent;
        Label SpeedProjected;
        Label SpeedAllowed;
        Label ControlMode;
        TrackMonitor Monitor;

        readonly Dictionary<Train.TRAIN_CONTROL, string> ControlModeLabels =
            new Dictionary<Train.TRAIN_CONTROL, string> {
			{ Train.TRAIN_CONTROL.AUTO_SIGNAL , "Auto Signal" },
			{ Train.TRAIN_CONTROL.AUTO_NODE, "Node" },
			{ Train.TRAIN_CONTROL.MANUAL, "Manual" },
            { Train.TRAIN_CONTROL.EXPLORER, "Explorer" },
			{ Train.TRAIN_CONTROL.OUT_OF_CONTROL, "OutOfControl : " },
			{ Train.TRAIN_CONTROL.UNDEFINED, "Unknown" },
		};

        static readonly Dictionary<Train.END_AUTHORITY, string> AuthorityLabels =
            new Dictionary<Train.END_AUTHORITY, string> {
			{ Train.END_AUTHORITY.END_OF_TRACK, "End Trck" },
			{ Train.END_AUTHORITY.END_OF_PATH, "End Path" },
			{ Train.END_AUTHORITY.RESERVED_SWITCH, "Switch" },
            { Train.END_AUTHORITY.LOOP, "Loop" },
			{ Train.END_AUTHORITY.TRAIN_AHEAD, "TrainAhd" },
			{ Train.END_AUTHORITY.MAX_DISTANCE, "Max Dist" },
			{ Train.END_AUTHORITY.NO_PATH_RESERVED, "No Path" },
            { Train.END_AUTHORITY.END_OF_AUTHORITY, "End Auth" },
		};

        static readonly Dictionary<Train.OUTOFCONTROL, string> OutOfControlLabels =
            new Dictionary<Train.OUTOFCONTROL, string> {
			{ Train.OUTOFCONTROL.SPAD, "SPAD" },
			{ Train.OUTOFCONTROL.SPAD_REAR, "SPAD-Rear" },
            { Train.OUTOFCONTROL.MISALIGNED_SWITCH, "Misalg Sw" },
			{ Train.OUTOFCONTROL.OUT_OF_AUTHORITY, "Off Auth" },
			{ Train.OUTOFCONTROL.OUT_OF_PATH, "Off Path" },
			{ Train.OUTOFCONTROL.SLIPPED_INTO_PATH, "Splipped" },
			{ Train.OUTOFCONTROL.SLIPPED_TO_ENDOFTRACK, "Slipped" },
			{ Train.OUTOFCONTROL.OUT_OF_TRACK, "Off Track" },
		};

        // Constructor
        public TrackMonitorWindow(WindowManager owner)
            : base(owner, 150, LabelsHeight + TrackMonitorHeight + 10, "Track Monitor")
        {
        }

        // Initialize
        protected internal override void Initialize()
        {
        }

        // Build Layout
        protected override ControlLayout Layout(ControlLayout layout)
        {
            // main box - add items in vertical order
            var vbox = base.Layout(layout).AddLayoutVertical();
            {

                // first text box - speed - items added in horizontal order
                var hbox_t1 = vbox.AddLayoutHorizontal(16);
                {
                    hbox_t1.Add(new Label(hbox_t1.RemainingWidth / 2, hbox_t1.RemainingHeight, "Speed:"));
                    hbox_t1.Add(SpeedCurrent = new Label(hbox_t1.RemainingWidth, hbox_t1.RemainingHeight, "", LabelAlignment.Right));
                }

                // second text box - projected speed - items added in horizontal order
                var hbox_t2 = vbox.AddLayoutHorizontal(16);
                {
                    hbox_t2.Add(new Label(hbox_t2.RemainingWidth / 2, hbox_t2.RemainingHeight, "Projected:"));
                    hbox_t2.Add(SpeedProjected = new Label(hbox_t2.RemainingWidth, hbox_t2.RemainingHeight, "", LabelAlignment.Right));
                }

                // third text bos - max allowed speed - items added in horizontal order
                var hbox_t3 = vbox.AddLayoutHorizontal(16);
                {
                    hbox_t3.Add(new Label(hbox_t3.RemainingWidth / 2, hbox_t3.RemainingHeight, "Limit:"));
                    hbox_t3.Add(SpeedAllowed = new Label(hbox_t3.RemainingWidth, hbox_t3.RemainingHeight, "", LabelAlignment.Right));
                }

                // add separator between speed and authority areas
                vbox.AddHorizontalSeparator();

                // first authority box : control mode
                var hbox_a1 = vbox.AddLayoutHorizontal(16);
                {
                    hbox_a1.Add(ControlMode = new Label(hbox_a1.RemainingWidth - 18, hbox_a1.RemainingHeight, "", LabelAlignment.Left));
                }

                // add separator between authority areas and object area header
                vbox.AddHorizontalSeparator();

                // add object area header
                var hbox_oh = vbox.AddLayoutHorizontal(16);
                hbox_oh.Add(new Label(hbox_oh.RemainingWidth, hbox_oh.RemainingHeight, " Dist      Speed   Aspect"));

                // add separator between object area header and object area
                vbox.AddHorizontalSeparator();

                // add object area
                vbox.Add(Monitor = new TrackMonitor(vbox.RemainingWidth, 50, Owner, this));
            }

            return vbox;
        }

        // Display
        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            // always get train details to pass on to Monitor
            Train.TrainInfo thisInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();
            Monitor.StoreInfo(thisInfo);


            // update text fields
            if (updateFull)
            {
                // speeds
                SpeedCurrent.Text = FormatStrings.FormatSpeed(Math.Abs(thisInfo.speedMpS), Owner.Viewer.MilepostUnitsMetric);
                SpeedProjected.Text = FormatStrings.FormatSpeed(Math.Abs(thisInfo.projectedSpeedMpS), Owner.Viewer.MilepostUnitsMetric);
                SpeedAllowed.Text = FormatStrings.FormatSpeed(thisInfo.allowedSpeedMpS, Owner.Viewer.MilepostUnitsMetric);

                // control mode
                string ControlText = ControlModeLabels[thisInfo.ControlMode];
                if (thisInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE)
                {
                    ControlText = FindAuthorityInfo(thisInfo.ObjectInfoForward, ControlText);
                }
                else if (thisInfo.ControlMode == Train.TRAIN_CONTROL.OUT_OF_CONTROL)
                {
                    ControlText = String.Concat(ControlText, OutOfControlLabels[thisInfo.ObjectInfoForward[0].OutOfControlReason]);
                }
                ControlMode.Text = String.Copy(ControlText);
            }
        }

        private string FindAuthorityInfo(List<Train.TrainObjectItem> ObjectInfo, string ControlText)
        {
            bool authorityFound = false;
            foreach (Train.TrainObjectItem thisInfo in ObjectInfo)
            {
                if (!authorityFound && thisInfo.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY)
                {
                    authorityFound = true;
                    return (String.Concat(ControlText, " : ", AuthorityLabels[thisInfo.AuthorityType]));
                }
            }

            return (ControlText);
        }
    }

    public class TrackMonitor : Control
    {
        TrackMonitorWindow parentWindow;
        static Texture2D SignalAspects;
        static Texture2D TrackMonitorImages;
        static Texture2D MonitorTexture;

        WindowTextFont Font;

        bool metric;

        Train.TrainInfo validInfo = null;

        // position constants
        readonly int addInfoOffset = 21; // vertical offset on window for additional out-of-range info at top and bottom
        readonly int[] mainOffset = new int[2] {16, 6 }; // offset for items, cell 0 is upward, 1 is downward
        readonly int textSpacing = 10; // minimum vertical distance between two labels

        readonly int distanceTextOffset = 4; // horizontal offset distance text
        readonly int speedTextOffset = 70; // horizontal offset distance text

        // position definition arrays
        // contents :
        // cell 0 : X offset
        // cell 1 : Y offset from top (absolute, but relative if Y position is variable)
        // cell 2 : Y offset from bottom (positive, value is distracted)
        // cell 3 : X size
        // cell 4 : Y size
        // cell 5 : Y offset between marker and text (positive is further from zeropoint)

        int[] eyePosition = new int[6] { 40, 0, 20, 24, 24, 0 };
        int[] TrainPosition = new int[6] { 38, 4, -12, 24, 24, 0 }; // Y value set in function
        int[] otherTrainPosition = new int[6] { 38, -16, 3, 24, 24, 0 }; // Y value set in function
        int[] StationPosition = new int[6] { 43, 0, 0, 15, 15, 0 }; // Y value set in function
        int[] ReversalPosition = new int[6] { 46, 15, 0, 10, 15, 0 }; // Y value set in function
        int[] endAuthorityPosition = new int[6] { 42, -2, 6, 16, 8, 0 }; // Y value set in function
        int[] SignalPosition = new int[6] { 130, 1, 0, 16, 16, -1 }; // Y value set in function
        int[] SignalTopPosition = new int[6] { 130, 24, 10, 16, 16, 1 };
        int[] arrowPosition = new int[6] { 26, -12, -11, 10, 24, 0 };

        // texture rectangles : X-offset, Y-offset, width, height
        Rectangle eyeTexture = new Rectangle(10, 394, 44, 44);
        Rectangle trainPositionAuto = new Rectangle(0, 192, 64, 64);
        Rectangle trainPositionManualOnRoute = new Rectangle(64, 256, 64, 64);
        Rectangle trainPositionManualOffRoute = new Rectangle(0, 256, 64, 64);
        Rectangle endAuthorityMarker = new Rectangle(0, 24, 64, 16);
        Rectangle oppositeTrainMarkerForward = new Rectangle(0, 320, 64, 64);
        Rectangle oppositeTrainMarkerBackward = new Rectangle(64, 320, 64, 64);
        Rectangle stationMarker = new Rectangle(64, 0, 64, 64);
        Rectangle reversalMarker = new Rectangle(0, 64, 64, 64);
        Rectangle forwardArrow = new Rectangle(74, 128, 44, 64);
        Rectangle backwardArrow = new Rectangle(10, 128, 44, 64);

        Dictionary<TrackMonitorSignalAspect, Rectangle> SignalMarkers =
            new Dictionary<TrackMonitorSignalAspect, Rectangle>
            { { TrackMonitorSignalAspect.Clear_2, new Rectangle (0, 0, 16, 16)},
              { TrackMonitorSignalAspect.Clear_1, new Rectangle (16, 0, 16, 16)},
              { TrackMonitorSignalAspect.Approach_3, new Rectangle (0, 16, 16, 16)},
              { TrackMonitorSignalAspect.Approach_2, new Rectangle (16, 16, 16, 16)},
              { TrackMonitorSignalAspect.Approach_1, new Rectangle (0, 32, 16, 16)},
              { TrackMonitorSignalAspect.Restricted, new Rectangle (16, 32, 16, 16)},
              { TrackMonitorSignalAspect.StopAndProceed, new Rectangle (0, 48, 16, 16)},
              { TrackMonitorSignalAspect.Stop, new Rectangle (16, 48, 16, 16)},
              { TrackMonitorSignalAspect.Permission, new Rectangle (0, 64, 16, 16)},
              { TrackMonitorSignalAspect.None, new Rectangle (16, 64, 16, 16)}};


        // fixed distance rounding values as function of maximum distance

        Dictionary<float, float> roundingValues =
            new Dictionary<float,float>
            {{0.0f, 0.5f},
             {5.0f, 1.0f},
             {10.0f, 2.0f}};

        // Constructor
        public TrackMonitor(int width, int height, WindowManager owner, TrackMonitorWindow thisWindow)
            : base(0, 0, width, height)
        {
            if (SignalAspects == null)
                SignalAspects = owner.Viewer.RenderProcess.Content.Load<Texture2D>("SignalAspects");
            if (TrackMonitorImages == null)
                TrackMonitorImages = owner.Viewer.RenderProcess.Content.Load<Texture2D>("TrackMonitorImages");

            metric = owner.Viewer.MilepostUnitsMetric;

            parentWindow = thisWindow;
            Font = owner.TextFontSmall;
        }

        // Draw
        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            // blank texture
            if (MonitorTexture == null)
            {
                MonitorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
                MonitorTexture.SetData(new[] { Color.White });
            }

            // no info available
            if (validInfo == null)
            {
                drawTrack(spriteBatch, offset, 0f, 1f);
                return;
            }

            // track lines
            drawTrack(spriteBatch, offset, validInfo.speedMpS, validInfo.allowedSpeedMpS);

            if (MultiPlayer.MPManager.IsMultiPlayer()) drawMPInfo(spriteBatch, offset);
            // info in AUTO node
            else if (validInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE || validInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_SIGNAL)
            {
                drawAutoInfo(spriteBatch, offset);
            }
            else
            {
                drawManualInfo(spriteBatch, offset);
            }
        }

        // Store train info
        public void StoreInfo(Train.TrainInfo thisInfo)
        {
            validInfo = thisInfo;
        }

        // draw track lines
        private void drawTrack(SpriteBatch spriteBatch, Point offset, float speedMpS, float allowedSpeedMpS)
        {
            float absspeedMpS = Math.Abs(speedMpS);
            var lineColor = (absspeedMpS < allowedSpeedMpS - 1.0f) ? Color.Green :
                ((absspeedMpS < allowedSpeedMpS) ? Color.PaleGreen :
                ((absspeedMpS < allowedSpeedMpS + 5.0f) ? Color.Orange : Color.Red));

            float lineStart = (float)TrackMonitorWindow.LabelsHeight + 2;
            float trackDistance = (float)TrackMonitorWindow.TrackMonitorHeight + 5;

            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 45, offset.Y + lineStart), null, lineColor, 0, Vector2.Zero,
                new Vector2(2, trackDistance), SpriteEffects.None, 0);
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 55, offset.Y + lineStart), null, lineColor, 0, Vector2.Zero,
                new Vector2(2, trackDistance), SpriteEffects.None, 0);
        }

        // draw auto info
        // all details accessed through class variables

        private void drawAutoInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            int offsetPosition = TrackMonitorWindow.LabelsHeight;
            int endOffset = TrackMonitorWindow.TrackMonitorHeight;
            int endPosition = offsetPosition + endOffset;
            int startObjectArea = offsetPosition + addInfoOffset + textSpacing;
            int endObjectArea = endPosition - addInfoOffset;

            float maxDistance = parentWindow.MAXDISTANCE;
            int zeropointmid = endObjectArea - textSpacing;
            int zeropointtop = zeropointmid - mainOffset[0]; // leave space for train symbol
            int zeropointlow = zeropointmid + mainOffset[1]; // leave space for train symbol
            float distanceFactor = (float)(zeropointtop - startObjectArea) / parentWindow.MAXDISTANCE;

            // draw line for object out of reach
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + offsetPosition + addInfoOffset), null, Color.White, 0, Vector2.Zero,
                new Vector2(142, 1), SpriteEffects.None, 0);

            // draw line for object behind
            // draw as red line if no info for reverse move available

            var lineColor = Color.White;
            if (validInfo.ObjectInfoBackward[0].ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY &&
                validInfo.ObjectInfoBackward[0].AuthorityType == Train.END_AUTHORITY.NO_PATH_RESERVED)
            {
                lineColor = Color.Red;
            }

            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + endPosition - addInfoOffset), null, lineColor, 0, Vector2.Zero,
                new Vector2(142, 1), SpriteEffects.None, 0);

            // draw own train marker
            drawOwnTrain(spriteBatch, offset, trainPositionAuto, zeropointtop + TrainPosition[1]);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                drawArrow(spriteBatch, offset, forwardArrow, zeropointlow + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                drawArrow(spriteBatch, offset, backwardArrow, zeropointlow + arrowPosition[1]);
            }

            // draw eye
            drawEye(spriteBatch, offset, offsetPosition, endPosition);

            // draw fixed distance indications
            float firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, maxDistance, distanceFactor, zeropointtop, 4, true);
            int firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeropointtop, zeropointlow, maxDistance, distanceFactor, firstLabelPosition, 
                validInfo.ObjectInfoForward, true);
        }

        // draw Multiplayer info
        // all details accessed through class variables

        private void drawMPInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            int offsetPosition = TrackMonitorWindow.LabelsHeight;
            int endOffset = TrackMonitorWindow.TrackMonitorHeight;
            int endPosition = offsetPosition + endOffset;
            int startObjectArea = offsetPosition + addInfoOffset + textSpacing;
            int endObjectArea = endPosition - addInfoOffset;

            float maxDistance = parentWindow.MAXDISTANCE;
            int zeropointmid = endObjectArea - textSpacing;
            if (validInfo.direction == 1) zeropointmid = startObjectArea - textSpacing;
            int zeropointtop = zeropointmid - mainOffset[0]; // leave space for train symbol
            int zeropointlow = zeropointmid + mainOffset[1]; // leave space for train symbol
            float distanceFactor = (float)(zeropointtop - startObjectArea) / parentWindow.MAXDISTANCE;
            if (validInfo.direction == 1) distanceFactor = (float)(endObjectArea - zeropointtop) / parentWindow.MAXDISTANCE;

            // draw line for object out of reach
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + offsetPosition + addInfoOffset), null, Color.White, 0, Vector2.Zero,
                new Vector2(142, 1), SpriteEffects.None, 0);

            // draw own train marker
            drawOwnTrain(spriteBatch, offset, trainPositionAuto, zeropointtop + TrainPosition[1]);

            // draw eye
            drawEye(spriteBatch, offset, offsetPosition, endPosition);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                drawArrow(spriteBatch, offset, forwardArrow, zeropointlow + arrowPosition[1]);
                // draw fixed distance indications
                float firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, maxDistance, distanceFactor, zeropointtop, 4, true);
                int firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

                // draw forward items
                drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeropointtop, zeropointlow, maxDistance, distanceFactor, firstLabelPosition,
                    validInfo.ObjectInfoForward, true);
            }
            else if (validInfo.direction == 1)
            {
                drawArrow(spriteBatch, offset, backwardArrow, zeropointtop + arrowPosition[2]);
                // draw fixed distance indications
                float firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, maxDistance, distanceFactor, zeropointlow, 4, false);
                int firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;
                // draw backward items
                drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeropointlow, zeropointtop, maxDistance, distanceFactor, firstLabelPosition,
                    validInfo.ObjectInfoBackward, false);
            }


        }

        // draw manual info
        // all details accessed through class variables

        private void drawManualInfo(SpriteBatch spriteBatch, Point offset)
        {
            // draw line for object beyond maximum distance
            int offsetPosition = TrackMonitorWindow.LabelsHeight;
            int endOffset = TrackMonitorWindow.TrackMonitorHeight;
            int endPosition = offsetPosition + endOffset;
            int startObjectArea = offsetPosition + addInfoOffset + textSpacing;
            int endObjectArea = endPosition - addInfoOffset;

            float maxDistance = parentWindow.MAXDISTANCE / 2;
            int zeropointmid = startObjectArea + (int)((endObjectArea - startObjectArea) / 2) - (textSpacing / 2);
            int zeropointtop = zeropointmid - mainOffset[0]; // leave space for train symbol
            int zeropointlow = zeropointmid + mainOffset[1]; // leave space for train symbol
            float distanceFactor = (float)(zeropointtop - startObjectArea) / maxDistance;

            // draw lines for objects beyond max distance forward and backward
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + offsetPosition + addInfoOffset), null, Color.White, 0, Vector2.Zero,
                new Vector2(142, 1), SpriteEffects.None, 0);

            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + endPosition - addInfoOffset), null, Color.White, 0, Vector2.Zero,
                new Vector2(142, 1), SpriteEffects.None, 0);

            // draw line through own train
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + zeropointmid), null, Color.White, 0, Vector2.Zero,
                new Vector2(142, 1), SpriteEffects.None, 0);

            // draw own train marker
            Rectangle ownTrainMarker = validInfo.isOnPath ? trainPositionManualOnRoute : trainPositionManualOffRoute;
            drawOwnTrain(spriteBatch, offset, ownTrainMarker, zeropointmid + TrainPosition[2]);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                drawArrow(spriteBatch, offset, forwardArrow, zeropointmid + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                drawArrow(spriteBatch, offset, backwardArrow, zeropointmid + arrowPosition[2]);
            }

            // draw eye
            drawEye(spriteBatch, offset, offsetPosition, endPosition);

            // draw fixed distance indications
            float firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, maxDistance, distanceFactor, zeropointtop, 3, true);
            drawDistanceMarkers(spriteBatch, offset, maxDistance, distanceFactor, zeropointlow, 3, false);  // no return required
            int firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeropointtop, zeropointlow, maxDistance, distanceFactor, firstLabelPosition,
                validInfo.ObjectInfoForward, true);

            // draw backward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeropointlow, zeropointtop, maxDistance, distanceFactor, firstLabelPosition, 
                validInfo.ObjectInfoBackward, false);
        }

        // draw own train marker at required position
        private void drawOwnTrain(SpriteBatch spriteBatch, Point offset, Rectangle marker, int position)
        {
            spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + TrainPosition[0], offset.Y + position,
                TrainPosition[3], TrainPosition[4]), marker, Color.White);
        }

        // draw own train marker at required position
        private void drawArrow(SpriteBatch spriteBatch, Point offset, Rectangle marker, int position)
        {
            spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + arrowPosition[0], offset.Y + position,
                arrowPosition[3], arrowPosition[4]), marker, Color.White);
        }

        // draw eye at required position
        private void drawEye(SpriteBatch spriteBatch, Point offset, int offsetPosition, int endPosition)
        {
            // draw eye
            if (validInfo.cabOrientation == 0)
            {
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + offsetPosition + eyePosition[1],
                    eyePosition[3], eyePosition[4]), eyeTexture, Color.White);
            }
            else
            {
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + endPosition - eyePosition[2],
                    eyePosition[3], eyePosition[4]), eyeTexture, Color.White);
            }
        }

        // draw fixed distance markers
        private float drawDistanceMarkers(SpriteBatch spriteBatch, Point offset,
            float maxDistance, float distanceFactor,int zeropoint, int noMarkers, bool forward)
        {
            float maxDistanceD = metric ? maxDistance / 1000 : Miles.FromM(maxDistance, metric); // in displayed units
            float markerIntervalD = maxDistanceD / noMarkers;

            float roundingValue = roundingValues[0];
            foreach (KeyValuePair<float, float> thisValue in roundingValues)
            {
                if (markerIntervalD > thisValue.Key) roundingValue = thisValue.Value;
            }

            markerIntervalD = Convert.ToInt32(markerIntervalD / roundingValue) * roundingValue;
            float markerIntervalM = metric ? markerIntervalD * 1000 : Miles.ToM(markerIntervalD, metric);

                for (int ipos = 1; ipos <= noMarkers; ipos++)
                {
                    float actDistanceD = markerIntervalD * ipos;
                    float actDistanceM = markerIntervalM * ipos;
                    if (actDistanceM < maxDistance)
                    {
                        int actLabelOffset = Convert.ToInt32(actDistanceM * distanceFactor);
                        int actLabelposition = forward ? zeropoint - actLabelOffset : zeropoint + actLabelOffset;
                        string distanceString = FormatStrings.FormatDistance(actDistanceM, metric);

                        Font.Draw(spriteBatch, new Point(offset.X + distanceTextOffset, offset.Y + actLabelposition), distanceString, Color.White);
                    }
                }

            return (markerIntervalM);
        }

        // draw signal, speed and authority items
        // items are sorted in order of increasing distance

        public void drawItems(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeropoint, int lastLabelPosition,
            float maxDistance, float distanceFactor, int firstLabelPosition, List<Train.TrainObjectItem> itemList, bool forward)
        {
            bool signalShown = false;
            bool firstLabelShown = false;

            foreach (Train.TrainObjectItem thisItem in itemList)
            {
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY:
                        drawAuthority(spriteBatch, offset, startObjectArea, endObjectArea, zeropoint,
                            maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        lastLabelPosition = drawSignal(spriteBatch, offset, startObjectArea, endObjectArea, zeropoint,
                            maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref signalShown, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST:
                        lastLabelPosition = drawSpeedpost(spriteBatch, offset, startObjectArea, endObjectArea, zeropoint,
                            maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.STATION:
                        drawStation(spriteBatch, offset, startObjectArea, endObjectArea, zeropoint,
                            maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL:
                        drawReversal(spriteBatch, offset, startObjectArea, endObjectArea, zeropoint,
                            maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem);
                        break;

                    default:     // capture unkown item
                        break;
                }
            }
        }

        // draw authority information
        public void drawAuthority(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeropoint,
                            float maxDistance, float distanceFactor, int firstLabelPosition, bool forward,
                            int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            Rectangle displayItem = new Rectangle(0, 0, 0, 0);
            bool displayRequired = false;
            int newLabelPosition = lastLabelPosition;
            int itemOffset = 2 * startObjectArea; // default is out of range
            int[] offsetArray = endAuthorityPosition;

            if (thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_AUTHORITY ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_PATH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_TRACK ||
                thisItem.AuthorityType == Train.END_AUTHORITY.RESERVED_SWITCH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.LOOP)
            {
                displayItem = endAuthorityMarker;
                displayRequired = true;
            }
            else if (thisItem.AuthorityType == Train.END_AUTHORITY.TRAIN_AHEAD)
            {
                displayItem = forward ? oppositeTrainMarkerForward : oppositeTrainMarkerBackward;
                offsetArray = otherTrainPosition;
                displayRequired = true;
            }

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor) && displayRequired)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                int itemLocation = forward ? zeropoint - itemOffset : zeropoint + itemOffset;
                int markerLocation = forward ? itemLocation + offsetArray[1] : itemLocation + offsetArray[2]; //adjust for difference in size between text and marker

                Rectangle markerPlacement = new Rectangle(offset.X + offsetArray[0], offset.Y + markerLocation,
                    offsetArray[3], offsetArray[4]);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);

                if (itemOffset < firstLabelPosition && !firstLabelShown)
                {
                    string distanceString = FormatStrings.FormatDistance(thisItem.DistanceToTrainM, metric);
                    Point labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + itemLocation);

                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }
        }


        // draw signal information
        public int drawSignal(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeropoint,
                            float maxDistance, float distanceFactor, int firstLabelPosition, bool forward,
                            int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool signalShown, ref bool firstLabelShown)
        {
            Rectangle displayItem = SignalMarkers[thisItem.SignalState];
            int newLabelPosition = lastLabelPosition;

            bool displayRequired = false;
            int itemLocation = 0;
            int itemOffset = 2 * startObjectArea; // default is out of range
            float maxDisplayDistance = maxDistance - (textSpacing / 2) / distanceFactor;

            if (thisItem.DistanceToTrainM < maxDisplayDistance)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                itemLocation = forward ? zeropoint - itemOffset - SignalPosition[1] : zeropoint + itemOffset - SignalPosition[2];
                displayRequired = true;
                signalShown = true;
            }
            else if (!signalShown)
            {
                itemLocation = forward ? startObjectArea - SignalTopPosition[1] : endObjectArea + SignalTopPosition[2]; // item is outside area
                signalShown = true;
                displayRequired = true;
            }

            if (displayRequired)
            {
                int reqMarkerPosition =
                    forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);
                newLabelPosition = reqMarkerPosition;

                int reqLabelPosition = reqMarkerPosition - SignalPosition[5];   //adjust for difference in size between text and marker
                Rectangle markerPlacement = new Rectangle(offset.X + SignalPosition[0], offset.Y + reqMarkerPosition,
                    SignalPosition[3], SignalPosition[4]);
                spriteBatch.Draw(SignalAspects, markerPlacement, displayItem, Color.White);

                if (thisItem.SignalState != TrackMonitorSignalAspect.Stop && thisItem.AllowedSpeedMpS > 0)
                {
                    string speedString = FormatStrings.FormatSpeed(thisItem.AllowedSpeedMpS, metric);
                    Point labelPoint = new Point(offset.X + speedTextOffset, offset.Y + reqLabelPosition);

                    Font.Draw(spriteBatch, labelPoint, speedString, Color.White);
                }

                if ( (itemOffset < firstLabelPosition && !firstLabelShown) || thisItem.DistanceToTrainM > maxDisplayDistance)
                {
                    string distanceString = FormatStrings.FormatDistance(thisItem.DistanceToTrainM, metric);
                    Point labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + reqLabelPosition);

                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return (newLabelPosition);
        }


        // draw speedpost information
        public int drawSpeedpost(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeropoint,
                            float maxDistance, float distanceFactor, int firstLabelPosition, bool forward,
                            int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            int newLabelPosition = lastLabelPosition;
            int itemOffset = 2 * startObjectArea; // default is out of range

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                int itemLocation = forward ? zeropoint - itemOffset : zeropoint + itemOffset;
                int reqLabelPosition =
                    forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);
                newLabelPosition = reqLabelPosition;

                string speedString = FormatStrings.FormatSpeed(thisItem.AllowedSpeedMpS, metric);
                Point labelPoint = new Point(offset.X + speedTextOffset, offset.Y + newLabelPosition);

                Font.Draw(spriteBatch, labelPoint, speedString, Color.White);

                if (itemOffset < firstLabelPosition && !firstLabelShown)
                {
                    string distanceString = FormatStrings.FormatDistance(thisItem.DistanceToTrainM, metric);
                    labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition);

                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return (newLabelPosition);
        }


        // draw station stop information
        public int drawStation(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeropoint,
                            float maxDistance, float distanceFactor, float firstLabelDistance, bool forward,
                            int lastLabelPosition, Train.TrainObjectItem thisItem)
        {
            Rectangle displayItem = stationMarker;
            int newLabelPosition = lastLabelPosition;
            int itemOffset = 2 * startObjectArea; // default is out of range

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                int itemLocation = forward ? zeropoint - itemOffset : zeropoint + itemOffset;
                Rectangle markerPlacement = new Rectangle(offset.X + StationPosition[0], offset.Y + itemLocation - StationPosition[1],
                    StationPosition[3], StationPosition[4]);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);
            }
            return (newLabelPosition);
        }

        // draw station stop information
        public int drawReversal(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeropoint,
                            float maxDistance, float distanceFactor, float firstLabelDistance, bool forward,
                            int lastLabelPosition, Train.TrainObjectItem thisItem)
        {
            Rectangle displayItem = reversalMarker;
            int newLabelPosition = lastLabelPosition;
            int itemOffset = 2 * startObjectArea; // default is out of range

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                int itemLocation = forward ? zeropoint - itemOffset : zeropoint + itemOffset;
                Rectangle markerPlacement = new Rectangle(offset.X + ReversalPosition[0], offset.Y + itemLocation - ReversalPosition[1],
                    ReversalPosition[3], ReversalPosition[4]);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);
            }
            return (newLabelPosition);
        }
    }

}
