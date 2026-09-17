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

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.Physics;
using ORTS.Common;
using ORTS.Settings;

namespace Orts.Viewer3D.Popups
{
    public class TrackMonitorWindow : Window
    {
        public const int MaximumDistance = 5000;
        public const int TrackMonitorLabelHeight = 130; // Height of labels above the main display.
        public const int TrackMonitorOffsetY = 25/*Window.DecorationOffset.Y*/ + TrackMonitorLabelHeight;
        const int TrackMonitorHeightInLinesOfText = 16;

        Label SpeedCurrent;
        Label SpeedProjected;
        Label SpeedAllowed;
        Label ControlMode;
        Label Gradient;
        public TrackMonitor Monitor { get; private set; }

        readonly Dictionary<Train.TRAIN_CONTROL, string> ControlModeLabels;

        static readonly Dictionary<Train.END_AUTHORITY, string> AuthorityLabels = new Dictionary<Train.END_AUTHORITY, string>
        {
			{ Train.END_AUTHORITY.END_OF_TRACK, "End Trck" },
			{ Train.END_AUTHORITY.END_OF_PATH, "End Path" },
			{ Train.END_AUTHORITY.RESERVED_SWITCH, "Switch" },
            { Train.END_AUTHORITY.LOOP, "Loop" },
			{ Train.END_AUTHORITY.TRAIN_AHEAD, "TrainAhd" },
			{ Train.END_AUTHORITY.MAX_DISTANCE, "Max Dist" },
			{ Train.END_AUTHORITY.NO_PATH_RESERVED, "No Path" },
            { Train.END_AUTHORITY.SIGNAL, "Signal" },
            { Train.END_AUTHORITY.END_OF_AUTHORITY, "End Auth" },
		};

        static readonly Dictionary<Train.OUTOFCONTROL, string> OutOfControlLabels = new Dictionary<Train.OUTOFCONTROL, string>
        {
			{ Train.OUTOFCONTROL.SPAD, "SPAD" },
			{ Train.OUTOFCONTROL.SPAD_REAR, "SPAD-Rear" },
            { Train.OUTOFCONTROL.MISALIGNED_SWITCH, "Misalg Sw" },
			{ Train.OUTOFCONTROL.OUT_OF_AUTHORITY, "Off Auth" },
			{ Train.OUTOFCONTROL.OUT_OF_PATH, "Off Path" },
			{ Train.OUTOFCONTROL.SLIPPED_INTO_PATH, "Splipped" },
			{ Train.OUTOFCONTROL.SLIPPED_TO_ENDOFTRACK, "Slipped" },
			{ Train.OUTOFCONTROL.OUT_OF_TRACK, "Off Track" },
            { Train.OUTOFCONTROL.SLIPPED_INTO_TURNTABLE, "Slip Turn" },
			{ Train.OUTOFCONTROL.UNDEFINED, "Undefined" },
		};

        public TrackMonitorWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 10, Window.DecorationSize.Y + owner.TextFontDefault.Height * (5 + TrackMonitorHeightInLinesOfText) + ControlLayout.SeparatorSize * 3, Viewer.Catalog.GetString("Track Monitor"))
        {
            ControlModeLabels = new Dictionary<Train.TRAIN_CONTROL, string> 
            {
			    { Train.TRAIN_CONTROL.AUTO_SIGNAL , Viewer.Catalog.GetString("Auto Signal") },
			    { Train.TRAIN_CONTROL.AUTO_NODE, Viewer.Catalog.GetString("Node") },
			    { Train.TRAIN_CONTROL.MANUAL, Viewer.Catalog.GetString("Manual") },
                { Train.TRAIN_CONTROL.EXPLORER, Viewer.Catalog.GetString("Explorer") },
			    { Train.TRAIN_CONTROL.OUT_OF_CONTROL, Viewer.Catalog.GetString("OutOfControl : ") },
                { Train.TRAIN_CONTROL.INACTIVE, Viewer.Catalog.GetString("Inactive") },
                { Train.TRAIN_CONTROL.TURNTABLE, Viewer.Catalog.GetString("Turntable") },
			    { Train.TRAIN_CONTROL.UNDEFINED, Viewer.Catalog.GetString("Unknown") },
		    };
        }

        public override void TabAction() => Monitor.CycleMode();

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, Viewer.Catalog.GetString("Speed:")));
                hbox.Add(SpeedCurrent = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, Viewer.Catalog.GetString("Projected:")));
                hbox.Add(SpeedProjected = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, Viewer.Catalog.GetString("Limit:")));
                hbox.Add(SpeedAllowed = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(ControlMode = new Label(hbox.RemainingWidth - 18, hbox.RemainingHeight, "", LabelAlignment.Left));
                hbox.Add(Gradient = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));

            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                hbox.Add(new Label(hbox.RemainingWidth, hbox.RemainingHeight, Viewer.Catalog.GetString(" Milepost   Limit     Dist")));
            }
            vbox.AddHorizontalSeparator();
            vbox.Add(Monitor = new TrackMonitor(vbox.RemainingWidth, vbox.RemainingHeight, Owner));

            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            // Always get train details to pass on to TrackMonitor.
            var thisInfo = Owner.Viewer.PlayerTrain.GetTrainInfo();
            Monitor.StoreInfo(thisInfo);

            // Update text fields on full update only.
            if (updateFull)
            {
                SpeedCurrent.Text = FormatStrings.FormatSpeedDisplay(Math.Abs(thisInfo.speedMpS), Owner.Viewer.MilepostUnitsMetric);
                SpeedProjected.Text = FormatStrings.FormatSpeedDisplay(Math.Abs(thisInfo.projectedSpeedMpS), Owner.Viewer.MilepostUnitsMetric);
                SpeedAllowed.Text = FormatStrings.FormatSpeedLimit(thisInfo.allowedSpeedMpS, Owner.Viewer.MilepostUnitsMetric);

                var ControlText = ControlModeLabels[thisInfo.ControlMode];
                if (thisInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE)
                {
                    ControlText = FindAuthorityInfo(thisInfo.ObjectInfoForward, ControlText);
                }
                else if (thisInfo.ControlMode == Train.TRAIN_CONTROL.OUT_OF_CONTROL)
                {
                    ControlText = String.Concat(ControlText, OutOfControlLabels[thisInfo.ObjectInfoForward[0].OutOfControlReason]);
                }
                ControlMode.Text = ControlText;
                if (-thisInfo.currentElevationPercent < -0.00015)
                {
                    var c = '\u2198';
                    Gradient.Text = String.Format("|  {0:F1}%{1} ", -thisInfo.currentElevationPercent, c);
                    Gradient.Color = Color.LightSkyBlue;
                }
                else if (-thisInfo.currentElevationPercent > 0.00015)
                {
                    var c = '\u2197';
                    Gradient.Text = String.Format("|  {0:F1}%{1} ", -thisInfo.currentElevationPercent, c);
                    Gradient.Color = Color.Yellow;
                }
                else Gradient.Text = "";
            }
        }

        static string FindAuthorityInfo(List<Train.TrainObjectItem> ObjectInfo, string ControlText)
        {
            foreach (var thisInfo in ObjectInfo)
            {
                if (thisInfo.ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY)
                {
                    // TODO: Concatenating strings is bad for localization.
                    return ControlText + " : " + AuthorityLabels[thisInfo.AuthorityType];
                }
            }

            return ControlText;
        }
    }

    public class TrackMonitor : Control
    {
        static Texture2D SignalAspects;
        static Texture2D TrackMonitorImages;
        static Texture2D MonitorTexture;

        WindowTextFont Font;

        readonly Viewer Viewer;
        private bool metric => Viewer.MilepostUnitsMetric;
        private readonly SavingProperty<int> StateProperty;
        public DisplayMode Mode
        {
            get => (DisplayMode)StateProperty.Value;
            private set
            {
                StateProperty.Value = (int)value;
            }
        }

        /// <summary>
        /// Different information views for the Track Monitor.
        /// </summary>
        public enum DisplayMode
        {
            /// <summary>
            /// Display all track and routing features.
            /// </summary>
            All = 0,
            /// <summary>
            /// Show only the static features that a train driver would know by memory.
            /// </summary>
            StaticOnly = 1,
        }

        public static int DbfEvalOverSpeed;//Debrief eval
        bool istrackColorRed = false;//Debrief eval
        public static Double DbfEvalOverSpeedTimeS = 0;//Debrief eval
        public static double DbfEvalIniOverSpeedTimeS = 0;//Debrief eval

        Train.TrainInfo validInfo;

        const int DesignWidth = 150; // All Width/X values are relative to this width.

        // position constants
        readonly int additionalInfoHeight = 16; // vertical offset on window for additional out-of-range info at top and bottom
        readonly int[] mainOffset = new int[2] { 12, 12 }; // offset for items, cell 0 is upward, 1 is downward
        readonly int textSpacing = 10; // minimum vertical distance between two labels

        // The track is 24 wide = 6 + 2 + 8 + 2 + 6.
        readonly int trackRail1Offset = 6;
        readonly int trackRail2Offset = 6 + 2 + 8;
        readonly int trackRailWidth = 2;

        // Vertical offset for text for forwards ([0]) and backwards ([1]).
        readonly int[] textOffset = new int[2] { -11, -3 };

        // Horizontal offsets for various elements.
        readonly int distanceTextOffset = 117;
        readonly int trackOffset = 42;
        readonly int speedTextOffset = 70;
        readonly int milepostTextOffset = 0;

        // position definition arrays
        // contents :
        // cell 0 : X offset
        // cell 1 : Y offset down from top (absolute)/item location (relative)
        // cell 2 : Y offset down from bottom (absolute)/item location (relative)
        // cell 3 : X size
        // cell 4 : Y size

        int[] eyePosition = new int[5] { 42, -4, -20, 24, 24 };
        int[] trainPosition = new int[5] { 42, -12, -12, 24, 24 }; // Relative positioning
        int[] otherTrainPosition = new int[5] { 42, -24, 0, 24, 24 }; // Relative positioning
        int[] stationPosition = new int[5] { 42, 0, -24, 24, 12 }; // Relative positioning
        int[] reversalPosition = new int[5] { 42, -21, -3, 24, 24 }; // Relative positioning
        int[] waitingPointPosition = new int[5] { 42, -21, -3, 24, 24 }; // Relative positioning
        int[] endAuthorityPosition = new int[5] { 42, -14, -10, 24, 24 }; // Relative positioning
        int[] signalPosition = new int[5] { 95, -16, 0, 16, 16 }; // Relative positioning
        int[] arrowPosition = new int[5] { 22, -12, -12, 24, 24 };
        int[] invalidReversalPosition = new int[5] { 42, -14, -10, 24, 24 }; // Relative positioning
        int[] leftSwitchPosition = new int[5] { 37, -14, -10, 24, 24 }; // Relative positioning
        int[] rightSwitchPosition = new int[5] { 47, -14, -10, 24, 24 }; // Relative positioning

        // texture rectangles : X-offset, Y-offset, width, height
        Rectangle eyeSprite = new Rectangle(0, 144, 24, 24);
        Rectangle trainPositionAutoForwardsSprite = new Rectangle(0, 72, 24, 24);
        Rectangle trainPositionAutoBackwardsSprite = new Rectangle(24, 72, 24, 24);
        Rectangle trainPositionManualOnRouteSprite = new Rectangle(24, 96, 24, 24);
        Rectangle trainPositionManualOffRouteSprite = new Rectangle(0, 96, 24, 24);
        Rectangle endAuthoritySprite = new Rectangle(0, 0, 24, 24);
        Rectangle oppositeTrainForwardSprite = new Rectangle(24, 120, 24, 24);
        Rectangle oppositeTrainBackwardSprite = new Rectangle(0, 120, 24, 24);
        Rectangle stationSprite = new Rectangle(24, 0, 24, 24);
        Rectangle reversalSprite = new Rectangle(0, 24, 24, 24);
        Rectangle waitingPointSprite = new Rectangle(24, 24, 24, 24);
        Rectangle forwardArrowSprite = new Rectangle(24, 48, 24, 24);
        Rectangle backwardArrowSprite = new Rectangle(0, 48, 24, 24);
        Rectangle invalidReversalSprite = new Rectangle(24, 144, 24, 24);
        Rectangle leftArrowSprite = new Rectangle(0, 168, 24, 24);
        Rectangle rightArrowSprite = new Rectangle(24, 168, 24, 24);

        Dictionary<TrackMonitorSignalAspect, Rectangle> SignalMarkers = new Dictionary<TrackMonitorSignalAspect, Rectangle>
        {
            { TrackMonitorSignalAspect.Clear_2, new Rectangle(0, 0, 16, 16) },
            { TrackMonitorSignalAspect.Clear_1, new Rectangle(16, 0, 16, 16) },
            { TrackMonitorSignalAspect.Approach_3, new Rectangle(0, 16, 16, 16) },
            { TrackMonitorSignalAspect.Approach_2, new Rectangle(16, 16, 16, 16) },
            { TrackMonitorSignalAspect.Approach_1, new Rectangle(0, 32, 16, 16) },
            { TrackMonitorSignalAspect.Restricted, new Rectangle(16, 32, 16, 16) },
            { TrackMonitorSignalAspect.StopAndProceed, new Rectangle(0, 48, 16, 16) },
            { TrackMonitorSignalAspect.Stop, new Rectangle(16, 48, 16, 16) },
            { TrackMonitorSignalAspect.Permission, new Rectangle(0, 64, 16, 16) },
            { TrackMonitorSignalAspect.None, new Rectangle(16, 64, 16, 16) }
        };

        // fixed distance rounding values as function of maximum distance
        Dictionary<float, float> roundingValues = new Dictionary<float, float>
        {
            { 0.0f, 0.5f },
            { 5.0f, 1.0f },
            { 10.0f, 2.0f }
        };

        public TrackMonitor(int width, int height, WindowManager owner)
            : base(0, 0, width, height)
        {
            if (SignalAspects == null)
                // TODO: This should happen on the loader thread.
                SignalAspects = SharedTextureManager.LoadInternal(owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(owner.Viewer.ContentPath, "SignalAspects.png"));
            if (TrackMonitorImages == null)
                // TODO: This should happen on the loader thread.
                TrackMonitorImages = SharedTextureManager.LoadInternal(owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(owner.Viewer.ContentPath, "TrackMonitorImages.png"));

            Viewer = owner.Viewer;
            StateProperty = Viewer.Settings.GetSavingProperty<int>("TrackMonitorDisplayMode");
            Font = owner.TextFontSmall;

            ScaleDesign(ref additionalInfoHeight);
            ScaleDesign(ref mainOffset);
            ScaleDesign(ref textSpacing);

            ScaleDesign(ref trackRail1Offset);
            ScaleDesign(ref trackRail2Offset);
            ScaleDesign(ref trackRailWidth);

            ScaleDesign(ref textOffset);

            ScaleDesign(ref distanceTextOffset);
            ScaleDesign(ref trackOffset);
            ScaleDesign(ref speedTextOffset);

            ScaleDesign(ref eyePosition);
            ScaleDesign(ref trainPosition);
            ScaleDesign(ref otherTrainPosition);
            ScaleDesign(ref stationPosition);
            ScaleDesign(ref reversalPosition);
            ScaleDesign(ref waitingPointPosition);
            ScaleDesign(ref endAuthorityPosition);
            ScaleDesign(ref signalPosition);
            ScaleDesign(ref arrowPosition);
            ScaleDesign(ref leftSwitchPosition);
            ScaleDesign(ref rightSwitchPosition);
            ScaleDesign(ref invalidReversalPosition);
        }

        /// <summary>
        /// Change the Track Monitor display mode.
        /// </summary>
        public void CycleMode()
        {
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    Mode = DisplayMode.StaticOnly;
                    break;
                case DisplayMode.StaticOnly:
                    Mode = DisplayMode.All;
                    break;
            }
        }

        void ScaleDesign(ref int variable)
        {
            variable = variable * Position.Width / DesignWidth;
        }

        void ScaleDesign(ref int[] variable)
        {
            for (var i = 0; i < variable.Length; i++)
                ScaleDesign(ref variable[i]);
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            if (MonitorTexture == null)
            {
                MonitorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                MonitorTexture.SetData(new[] { Color.White });
            }

            // Adjust offset to point at the control's position so we can keep code below simple.
            offset.X += Position.X;
            offset.Y += Position.Y;

            if (validInfo == null)
            {
                drawTrack(spriteBatch, offset, 0f, 1f);
                return;
            }

            drawTrack(spriteBatch, offset, validInfo.speedMpS, validInfo.allowedSpeedMpS);

            if (Orts.MultiPlayer.MPManager.IsMultiPlayer())
            {
                drawMPInfo(spriteBatch, offset);
            }
            else if (validInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE || validInfo.ControlMode == Train.TRAIN_CONTROL.AUTO_SIGNAL)
            {
                drawAutoInfo(spriteBatch, offset);
            }
            else if (validInfo.ControlMode == Train.TRAIN_CONTROL.TURNTABLE) return;
            else
            {
                drawManualInfo(spriteBatch, offset);
            }
        }

        public void StoreInfo(Train.TrainInfo thisInfo)
        {
            validInfo = thisInfo;
        }

        void drawTrack(SpriteBatch spriteBatch, Point offset, float speedMpS, float allowedSpeedMpS)
        {
            var train = Program.Viewer.PlayerLocomotive.Train;
            var absoluteSpeedMpS = Math.Abs(speedMpS);
            var trackColor =
                absoluteSpeedMpS < allowedSpeedMpS - 1.0f ? Color.Green :
                absoluteSpeedMpS < allowedSpeedMpS + 0.0f ? Color.PaleGreen :
                absoluteSpeedMpS < allowedSpeedMpS + 5.0f ? Color.Orange : Color.Red;

            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X + trackOffset + trackRail1Offset, offset.Y, trackRailWidth, Position.Height), trackColor);
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X + trackOffset + trackRail2Offset, offset.Y, trackRailWidth, Position.Height), trackColor);

            if (trackColor == Color.Red && !istrackColorRed)//Debrief Eval
            {
                istrackColorRed = true;
                DbfEvalIniOverSpeedTimeS = Orts.MultiPlayer.MPManager.Simulator.ClockTime;
            }            

            if (istrackColorRed && trackColor != Color.Red)//Debrief Eval
            {
                istrackColorRed = false;
                DbfEvalOverSpeed++;
            }

            if (istrackColorRed && (Orts.MultiPlayer.MPManager.Simulator.ClockTime - DbfEvalIniOverSpeedTimeS) > 1.0000)//Debrief Eval
            {
                DbfEvalOverSpeedTimeS = DbfEvalOverSpeedTimeS + (Orts.MultiPlayer.MPManager.Simulator.ClockTime - DbfEvalIniOverSpeedTimeS);
                train.DbfEvalValueChanged = true;
                DbfEvalIniOverSpeedTimeS = Orts.MultiPlayer.MPManager.Simulator.ClockTime;
            }
        }

        void drawAutoInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Position.Height - additionalInfoHeight - trainPosition[4];
            var zeroObjectPointTop = endObjectArea;
            var zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
            var zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            var distanceFactor = (float)(endObjectArea - startObjectArea) / TrackMonitorWindow.MaximumDistance;

            // draw train position line
            // use red if no info for reverse move available
            var lineColor = Color.DarkGray;
            if (validInfo.ObjectInfoBackward != null && validInfo.ObjectInfoBackward.Count > 0 &&
                validInfo.ObjectInfoBackward[0].ItemType == Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY &&
                validInfo.ObjectInfoBackward[0].AuthorityType == Train.END_AUTHORITY.NO_PATH_RESERVED)
            {
                lineColor = Color.Red;
            }
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + endObjectArea, Position.Width, 1), lineColor);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                drawArrow(spriteBatch, offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                drawArrow(spriteBatch, offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            // draw eye
            drawEye(spriteBatch, offset, 0, Position.Height);

            // draw fixed distance indications
            var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 4, true);
            var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);

            // draw own train marker
            drawOwnTrain(spriteBatch, offset, trainPositionAutoForwardsSprite, zeroObjectPointTop);
        }

        // draw Multiplayer info
        // all details accessed through class variables

        void drawMPInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Position.Height - additionalInfoHeight;
            var zeroObjectPointTop = 0;
            var zeroObjectPointMiddle = 0;
            var zeroObjectPointBottom = 0;
            if (validInfo.direction == 0)
            {
                zeroObjectPointTop = endObjectArea - trainPosition[4];
                zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            else if (validInfo.direction == 1)
            {
                zeroObjectPointTop = startObjectArea;
                zeroObjectPointMiddle = zeroObjectPointTop - trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            else
            {
                zeroObjectPointMiddle = startObjectArea + (endObjectArea - startObjectArea) / 2;
                zeroObjectPointTop = zeroObjectPointMiddle + trainPosition[1];
                zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            }
            var distanceFactor = (float)(endObjectArea - startObjectArea - trainPosition[4]) / TrackMonitorWindow.MaximumDistance;
            if (validInfo.direction == -1)
                distanceFactor /= 2;

            if (validInfo.direction == 0)
            {
                // draw direction arrow
                drawArrow(spriteBatch, offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                // draw direction arrow
                drawArrow(spriteBatch, offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            if (validInfo.direction != 1)
            {
                // draw fixed distance indications
                var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 4, true);
                var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

                // draw forward items
                drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);
            }

            if (validInfo.direction != 0)
            {
                // draw fixed distance indications
                var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointBottom, 4, false);
                var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

                // draw backward items
                drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointBottom, zeroObjectPointTop, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoBackward, false);
            }

            // draw own train marker
            drawOwnTrain(spriteBatch, offset, validInfo.direction == -1 ? trainPositionManualOnRouteSprite : validInfo.direction == 0 ? trainPositionAutoForwardsSprite : trainPositionAutoBackwardsSprite, zeroObjectPointTop);
        }

        // draw manual info
        // all details accessed through class variables

        void drawManualInfo(SpriteBatch spriteBatch, Point offset)
        {
            // set area details
            var startObjectArea = additionalInfoHeight;
            var endObjectArea = Position.Height - additionalInfoHeight;
            var zeroObjectPointMiddle = startObjectArea + (endObjectArea - startObjectArea) / 2;
            var zeroObjectPointTop = zeroObjectPointMiddle + trainPosition[1];
            var zeroObjectPointBottom = zeroObjectPointMiddle - trainPosition[2];
            var distanceFactor = (float)(zeroObjectPointTop - startObjectArea) / TrackMonitorWindow.MaximumDistance;

            // draw lines through own train
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + zeroObjectPointTop, Position.Width, 1), Color.DarkGray);
            spriteBatch.Draw(MonitorTexture, new Rectangle(offset.X, offset.Y + zeroObjectPointBottom - 1, Position.Width, 1), Color.DarkGray);

            // draw direction arrow
            if (validInfo.direction == 0)
            {
                drawArrow(spriteBatch, offset, forwardArrowSprite, zeroObjectPointMiddle + arrowPosition[1]);
            }
            else if (validInfo.direction == 1)
            {
                drawArrow(spriteBatch, offset, backwardArrowSprite, zeroObjectPointMiddle + arrowPosition[2]);
            }

            // draw eye
            drawEye(spriteBatch, offset, 0, Position.Height);

            // draw fixed distance indications
            var firstMarkerDistance = drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointTop, 3, true);
            drawDistanceMarkers(spriteBatch, offset, TrackMonitorWindow.MaximumDistance, distanceFactor, zeroObjectPointBottom, 3, false);  // no return required
            var firstLabelPosition = Convert.ToInt32(firstMarkerDistance * distanceFactor) - textSpacing;

            // draw forward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointTop, zeroObjectPointBottom, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoForward, true);

            // draw backward items
            drawItems(spriteBatch, offset, startObjectArea, endObjectArea, zeroObjectPointBottom, zeroObjectPointTop, TrackMonitorWindow.MaximumDistance, distanceFactor, firstLabelPosition, validInfo.ObjectInfoBackward, false);

            // draw own train marker
            var ownTrainSprite = validInfo.isOnPath ? trainPositionManualOnRouteSprite : trainPositionManualOffRouteSprite;
            drawOwnTrain(spriteBatch, offset, ownTrainSprite, zeroObjectPointTop);
        }

        // draw own train marker at required position
        void drawOwnTrain(SpriteBatch spriteBatch, Point offset, Rectangle sprite, int position)
        {
            spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + trainPosition[0], offset.Y + position, trainPosition[3], trainPosition[4]), sprite, Color.White);
        }

        // draw arrow at required position
        void drawArrow(SpriteBatch spriteBatch, Point offset, Rectangle sprite, int position)
        {
            spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + arrowPosition[0], offset.Y + position, arrowPosition[3], arrowPosition[4]), sprite, Color.White);
        }

        // draw eye at required position
        void drawEye(SpriteBatch spriteBatch, Point offset, int forwardsY, int backwardsY)
        {
            // draw eye
            if (validInfo.cabOrientation == 0)
            {
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + forwardsY + eyePosition[1], eyePosition[3], eyePosition[4]), eyeSprite, Color.White);
            }
            else
            {
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + eyePosition[0], offset.Y + backwardsY + eyePosition[2], eyePosition[3], eyePosition[4]), eyeSprite, Color.White);
            }
        }

        // draw fixed distance markers
        float drawDistanceMarkers(SpriteBatch spriteBatch, Point offset, float maxDistance, float distanceFactor, int zeroPoint, int numberOfMarkers, bool forward)
        {
            var maxDistanceD = Me.FromM(maxDistance, metric); // in displayed units
            var markerIntervalD = maxDistanceD / numberOfMarkers;

            var roundingValue = roundingValues[0];
            foreach (var thisValue in roundingValues)
            {
                if (markerIntervalD > thisValue.Key)
                {
                    roundingValue = thisValue.Value;
                }
            }

            markerIntervalD = Convert.ToInt32(markerIntervalD / roundingValue) * roundingValue;
            var markerIntervalM = Me.ToM(markerIntervalD, metric);  // from display back to metre

            for (var ipos = 1; ipos <= numberOfMarkers; ipos++)
            {
                var actDistanceM = markerIntervalM * ipos;
                if (actDistanceM < maxDistance)
                {
                    var itemOffset = Convert.ToInt32(actDistanceM * distanceFactor);
                    var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                    var distanceString = FormatStrings.FormatDistanceDisplay(actDistanceM, metric);
                    Font.Draw(spriteBatch, new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]), distanceString, Color.White);
                }
            }

            return markerIntervalM;
        }

        // draw signal, speed and authority items
        // items are sorted in order of increasing distance

        void drawItems(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, int lastLabelPosition, float maxDistance, float distanceFactor, int firstLabelPosition, List<Train.TrainObjectItem> itemList, bool forward)
        {
            var signalShown = false;
            var firstLabelShown = false;
            var borderSignalShown = false;

            foreach (var thisItem in itemList)
            {
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY:
                        drawAuthority(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        lastLabelPosition = drawSignalForward(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref signalShown, ref borderSignalShown, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEED_SIGNAL:
                        lastLabelPosition = drawSpeedpost(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST:
                        lastLabelPosition = drawSpeedpost(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.STATION:
                        drawStation(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.WAITING_POINT:
                        drawWaitingPoint(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.MILEPOST:
                        lastLabelPosition = drawMilePost(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH:
                        drawSwitch(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL:
                        drawReversal(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    default:     // capture unkown item
                        break;
                }
            }
            //drawReversal and drawSwitch icons on top.
            foreach (var thisItem in itemList)
            {
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.FACING_SWITCH:
                        drawSwitch(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.REVERSAL:
                        drawReversal(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, firstLabelPosition, forward, lastLabelPosition, thisItem, ref firstLabelShown);
                        break;

                    default:
                        break;
                }
            }
            // reverse display of signals to have correct superposition
            for (int iItems = itemList.Count-1 ; iItems >=0; iItems--)
            {
                var thisItem = itemList[iItems];
                switch (thisItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        drawSignalBackward(spriteBatch, offset, startObjectArea, endObjectArea, zeroPoint, maxDistance, distanceFactor, forward, thisItem, signalShown);
                        break;

                    default:
                        break;
                }
            }
        }

        // draw authority information
        void drawAuthority(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = new Rectangle(0, 0, 0, 0);
            var displayRequired = false;
            var offsetArray = new int[0];

            if (thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_AUTHORITY ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_PATH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.END_OF_TRACK ||
                thisItem.AuthorityType == Train.END_AUTHORITY.RESERVED_SWITCH ||
                thisItem.AuthorityType == Train.END_AUTHORITY.LOOP)
            {
                displayItem = endAuthoritySprite;
                offsetArray = endAuthorityPosition;
                displayRequired = true;
            }
            else if (thisItem.AuthorityType == Train.END_AUTHORITY.TRAIN_AHEAD)
            {
                displayItem = forward ? oppositeTrainForwardSprite : oppositeTrainBackwardSprite;
                offsetArray = otherTrainPosition;
                displayRequired = true;
            }

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor) && displayRequired)
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                spriteBatch.Draw(TrackMonitorImages, new Rectangle(offset.X + offsetArray[0], offset.Y + itemLocation + offsetArray[forward ? 1 : 2], offsetArray[3], offsetArray[4]), displayItem, Color.White);

                if (itemOffset < firstLabelPosition && !firstLabelShown)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }
        }

        // check signal information for reverse display
        int drawSignalForward(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool signalShown, ref bool borderSignalShown, ref bool firstLabelShown)
        {
            var displayItem = SignalMarkers[thisItem.SignalState];
            var newLabelPosition = lastLabelPosition;

            var displayRequired = false;
            var itemLocation = 0;
            var itemOffset = 0;
            var maxDisplayDistance = maxDistance - (textSpacing / 2) / distanceFactor;

            if (thisItem.DistanceToTrainM < maxDisplayDistance)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                displayRequired = true;
                signalShown = true;
            }
            else if (!borderSignalShown && !signalShown)
            {
                itemOffset = 2 * startObjectArea;
                itemLocation = forward ? startObjectArea : endObjectArea;
                displayRequired = true;
                borderSignalShown = true;
            }

            bool showSpeeds;
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    showSpeeds = true;
                    break;
                case DisplayMode.StaticOnly:
                    showSpeeds = false;
                    break;
            }

            if (displayRequired)
            {
                if (showSpeeds && thisItem.SignalState != TrackMonitorSignalAspect.Stop && thisItem.AllowedSpeedMpS > 0)
                {
                    var labelPoint = new Point(offset.X + speedTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    var speedString = FormatStrings.FormatSpeedLimitNoUoM(thisItem.AllowedSpeedMpS, metric);
                    Font.Draw(spriteBatch, labelPoint, speedString, Color.White);
                }

                if ((itemOffset < firstLabelPosition && !firstLabelShown) || thisItem.DistanceToTrainM > maxDisplayDistance)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + itemLocation + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }

        // draw signal information
        void drawSignalBackward(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, bool forward, Train.TrainObjectItem thisItem, bool signalShown)
        {
            TrackMonitorSignalAspect aspect;
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    aspect = thisItem.SignalState;
                    break;
                case DisplayMode.StaticOnly:
                    aspect = TrackMonitorSignalAspect.None;
                    break;
            }
            var displayItem = SignalMarkers[aspect];
 
            var displayRequired = false;
            var itemLocation = 0;
            var itemOffset = 0;
            var maxDisplayDistance = maxDistance - (textSpacing / 2) / distanceFactor;

            if (thisItem.DistanceToTrainM < maxDisplayDistance)
            {
                itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                displayRequired = true;
            }
            else if (!signalShown)
            {
                itemOffset = 2 * startObjectArea;
                itemLocation = forward ? startObjectArea : endObjectArea;
                displayRequired = true;
            }

            if (displayRequired)
            {
                spriteBatch.Draw(SignalAspects, new Rectangle(offset.X + signalPosition[0], offset.Y + itemLocation + signalPosition[forward ? 1 : 2], signalPosition[3], signalPosition[4]), displayItem, Color.White);
            }

        }

        // draw speedpost information
        int drawSpeedpost(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                var allowedSpeed = thisItem.AllowedSpeedMpS;
                if (allowedSpeed > 998)
                {
                    if (!Program.Simulator.TimetableMode)
                    {
                        allowedSpeed = (float)Program.Simulator.TRK.Tr_RouteFile.SpeedLimit;
                    }
                }

                var labelPoint = new Point(offset.X + speedTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                var speedString = FormatStrings.FormatSpeedLimitNoUoM(allowedSpeed, metric);
                Font.Draw(spriteBatch, labelPoint, speedString, thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.Standard ? (thisItem.IsWarning ? Color.Yellow : Color.White) :
                    (thisItem.SpeedObjectType == Train.TrainObjectItem.SpeedItemType.TempRestrictedStart ? Color.Red : Color.LightGreen));

                if (itemOffset < firstLabelPosition && !firstLabelShown)
                {
                    labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }


        // draw station stop information
        int drawStation(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem)
        {
            var displayItem = stationSprite;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                var startOfPlatform = (int)Math.Max(stationPosition[4], thisItem.StationPlatformLength * distanceFactor);
                var markerPlacement = new Rectangle(offset.X + stationPosition[0], offset.Y + itemLocation + stationPosition[forward ? 1 : 2], stationPosition[3], startOfPlatform);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);
            }

            return newLabelPosition;
        }

        // draw reversal information
        int drawReversal(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = thisItem.Valid ? reversalSprite : invalidReversalSprite;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                // What was this offset all about? Shouldn't we draw the icons in the correct location ALL the time? -- James Ross
                // var correctingOffset = Program.Simulator.TimetableMode || !Program.Simulator.Settings.EnhancedActCompatibility ? 0 : 7;

                if (thisItem.Valid)
                {
                    var markerPlacement = new Rectangle(offset.X + reversalPosition[0], offset.Y + itemLocation + reversalPosition[forward ? 1 : 2], reversalPosition[3], reversalPosition[4]);
                    spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, thisItem.Enabled ? Color.LightGreen : Color.White);
                }
                else
                {
                    var markerPlacement = new Rectangle(offset.X + invalidReversalPosition[0], offset.Y + itemLocation + invalidReversalPosition[forward ? 1 : 2], invalidReversalPosition[3], invalidReversalPosition[4]);
                    spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);
                }

                // Only show distance for enhanced MSTS compatibility (this is the only time the position is controlled by the author).
                if (itemOffset < firstLabelDistance && !firstLabelShown && !Program.Simulator.TimetableMode)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }

        // draw waiting point information
        int drawWaitingPoint(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = waitingPointSprite;
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                var markerPlacement = new Rectangle(offset.X + waitingPointPosition[0], offset.Y + itemLocation + waitingPointPosition[forward ? 1 : 2], waitingPointPosition[3], waitingPointPosition[4]);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, thisItem.Enabled ? Color.Yellow : Color.Red);

                if (itemOffset < firstLabelDistance && !firstLabelShown)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }

        // draw milepost information
        int drawMilePost(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, int firstLabelPosition, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var newLabelPosition = lastLabelPosition;

            if (thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);
                var labelPoint = new Point(offset.X + milepostTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                var milepostString = thisItem.ThisMile;
                Font.Draw(spriteBatch, labelPoint, milepostString, Color.White);

            }

            return newLabelPosition;
        }

        // draw switch information
        int drawSwitch(SpriteBatch spriteBatch, Point offset, int startObjectArea, int endObjectArea, int zeroPoint, float maxDistance, float distanceFactor, float firstLabelDistance, bool forward, int lastLabelPosition, Train.TrainObjectItem thisItem, ref bool firstLabelShown)
        {
            var displayItem = thisItem.IsRightSwitch ? rightArrowSprite : leftArrowSprite;
            var newLabelPosition = lastLabelPosition;

            bool showSwitches;
            switch (Mode)
            {
                case DisplayMode.All:
                default:
                    showSwitches = true;
                    break;
                case DisplayMode.StaticOnly:
                    showSwitches = false;
                    break;
            }

            if (showSwitches && thisItem.DistanceToTrainM < (maxDistance - textSpacing / distanceFactor))
            {
                var itemOffset = Convert.ToInt32(thisItem.DistanceToTrainM * distanceFactor);
                var itemLocation = forward ? zeroPoint - itemOffset : zeroPoint + itemOffset;
                newLabelPosition = forward ? Math.Min(itemLocation, lastLabelPosition - textSpacing) : Math.Max(itemLocation, lastLabelPosition + textSpacing);

                var markerPlacement = thisItem.IsRightSwitch ?
                    new Rectangle(offset.X + rightSwitchPosition[0], offset.Y + itemLocation + rightSwitchPosition[forward ? 1 : 2], rightSwitchPosition[3], rightSwitchPosition[4]) :
                    new Rectangle(offset.X + leftSwitchPosition[0], offset.Y + itemLocation + leftSwitchPosition[forward ? 1 : 2], leftSwitchPosition[3], leftSwitchPosition[4]);
                spriteBatch.Draw(TrackMonitorImages, markerPlacement, displayItem, Color.White);

                // Only show distance for enhanced MSTS compatibility (this is the only time the position is controlled by the author).
                if (itemOffset < firstLabelDistance && !firstLabelShown && !Program.Simulator.TimetableMode)
                {
                    var labelPoint = new Point(offset.X + distanceTextOffset, offset.Y + newLabelPosition + textOffset[forward ? 0 : 1]);
                    var distanceString = FormatStrings.FormatDistanceDisplay(thisItem.DistanceToTrainM, metric);
                    Font.Draw(spriteBatch, labelPoint, distanceString, Color.White);
                    firstLabelShown = true;
                }
            }

            return newLabelPosition;
        }


    }
}
