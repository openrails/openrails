// COPYRIGHT 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 
//

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ORTS.Popups
{
    public enum TrackMonitorSignalAspect
    {
        None,
        Clear,
        Warning,
        Stop,
    }

    public class TrackMonitorWindow : Window
    {
        const int MAXLINES = 20;
        const int MAXDISTANCE = 5000;
        const int TrackMonitorHeight = 260;
        const int LabelsHeight = 117;

        Label SpeedCurrent;
        Label SpeedProjected;
        Label SpeedAllowed;
        Label POILabel;
        Label POIDistance;
        Label ListHead;
        Label [] DistanceList = new Label[MAXLINES];
        Label [] PathList     = new Label[MAXLINES];
        Label [] SpeedList    = new Label[MAXLINES];
        Image [] SignalList   = new Image[MAXLINES];
        bool [] LineSet = new bool[MAXLINES];
        TrackMonitor Monitor;

        float LastSpeedMpS;
        SmoothedData AccelerationMpSpS = new SmoothedData();

        static Texture2D SignalAspects;
        static readonly Dictionary<TrackMonitorSignalAspect, Rectangle> SignalAspectSources = InitSignalAspectSources();
        static Dictionary<TrackMonitorSignalAspect, Rectangle> InitSignalAspectSources()
        {
            return new Dictionary<TrackMonitorSignalAspect, Rectangle> {
                                { TrackMonitorSignalAspect.None, new Rectangle(0, 0, 16, 16) },
                                { TrackMonitorSignalAspect.Clear, new Rectangle(16, 0, 16, 16) },
                                { TrackMonitorSignalAspect.Warning, new Rectangle(0, 16, 16, 16) },
                                { TrackMonitorSignalAspect.Stop, new Rectangle(16, 16, 16, 16) },
                        };
        }

        static readonly Dictionary<DispatcherPOIType, string> DispatcherPOILabels = InitDispatcherPOILabels();
        static Dictionary<DispatcherPOIType, string> InitDispatcherPOILabels()
        {
            return new Dictionary<DispatcherPOIType, string> {
                                { DispatcherPOIType.Unknown, "" },
                                { DispatcherPOIType.OffPath, "Off Path" },
                                { DispatcherPOIType.StationStop, "Station:" },
                                { DispatcherPOIType.ReversePoint, "Reverser:" },
                                { DispatcherPOIType.EndOfAuthorization, "End of Auth:" },
                                { DispatcherPOIType.Stop, "Stop:" },
                        };
        }

        public TrackMonitorWindow(WindowManager owner)
            : base(owner, 150, LabelsHeight + TrackMonitorHeight + 15, "Track Monitor")
        {
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (SignalAspects == null)
                SignalAspects = Owner.Viewer.RenderProcess.Content.Load<Texture2D>("SignalAspects");
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontal(16);
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "Speed:"));
                hbox.Add(SpeedCurrent = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            {
                var hbox = vbox.AddLayoutHorizontal(16);
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "Projected:"));
                hbox.Add(SpeedProjected = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            {
                var hbox = vbox.AddLayoutHorizontal(16);
                hbox.Add(new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "Limit:"));
                hbox.Add(SpeedAllowed = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontal(16);
                hbox.Add(POILabel = new Label(hbox.RemainingWidth / 2, hbox.RemainingHeight, "POI:"));
                hbox.Add(POIDistance = new Label(hbox.RemainingWidth - 18, hbox.RemainingHeight, "0m", LabelAlignment.Right));
            }
            vbox.AddHorizontalSeparator();
            {
                var hbox = vbox.AddLayoutHorizontal(16);
                hbox.Add(new Label(hbox.RemainingWidth , hbox.RemainingHeight, " Dist      Speed   Aspect"));
                hbox.Add(ListHead = new Label(hbox.RemainingWidth, hbox.RemainingHeight, "", LabelAlignment.Right));
            }

            vbox.AddHorizontalSeparator();
            vbox.Add(Monitor = new TrackMonitor(vbox.RemainingWidth, 50, MAXDISTANCE, TrackMonitorHeight, LabelsHeight, Owner.Viewer.MilepostUnitsMetric,
                Owner.Viewer.RenderProcess.Content.Load<Texture2D>("Train_TM"), SignalAspects, Owner.Viewer.PlayerTrain.SignalObjectItems));

            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            var speedMpS = Owner.Viewer.PlayerLocomotive.SpeedMpS;
            var allowedSpeedMpS = Owner.Viewer.PlayerTrain.AllowedMaxSpeedMpS;
            if (elapsedTime.ClockSeconds < AccelerationMpSpS.SmoothPeriodS)
                AccelerationMpSpS.Update(elapsedTime.ClockSeconds, (speedMpS - LastSpeedMpS) / elapsedTime.ClockSeconds);
            LastSpeedMpS = speedMpS;

            if (updateFull)
            {
                var poiDistance = 0f;
                var poiBackwards = false;
                var poiType = Owner.Viewer.Simulator.AI.Dispatcher.GetPlayerNextPOI(out poiDistance, out poiBackwards);
                var milepostUnitsMetric = Owner.Viewer.MilepostUnitsMetric;
                var signalDistance = Owner.Viewer.PlayerTrain.distanceToSignal;
                var signalAspect = Owner.Viewer.PlayerTrain.TMaspect;

                var speedProjectedMpS = speedMpS + 60 * AccelerationMpSpS.SmoothedValue;
                speedProjectedMpS = speedMpS > float.Epsilon ? Math.Max(0, speedProjectedMpS) : speedMpS < -float.Epsilon ? Math.Min(0, speedProjectedMpS) : 0;
                SpeedCurrent.Text = FormatSpeed(speedMpS, milepostUnitsMetric);
                SpeedProjected.Text = FormatSpeed(speedProjectedMpS, milepostUnitsMetric);
                SpeedAllowed.Text = FormatSpeed(allowedSpeedMpS, milepostUnitsMetric);

                POILabel.Text = DispatcherPOILabels[poiType];
                POIDistance.Text = poiType == DispatcherPOIType.Unknown || poiType == DispatcherPOIType.OffPath ? "" : FormatDistance(poiDistance, milepostUnitsMetric);
                Monitor.trackObjects = Owner.Viewer.PlayerTrain.SignalObjectItems;
                Monitor.OverSpeed = (speedMpS > allowedSpeedMpS) ? true : false;
                Monitor.OLSignalDistance = (signalAspect == TrackMonitorSignalAspect.None) ? 0f : signalDistance;
                Monitor.OLSignalAspect = signalAspect;
            }
        }

        public static string FormatSpeed(float speed, bool metric)
        {
            return String.Format(metric ? "{0:F1}kph" : "{0:F1}mph", MpS.FromMpS(speed, metric));
        }

        public static string FormatDistance(float distance, bool metric)
        {
            if (metric)
            {
                // <0.1 kilometers, show meters.
                if (Math.Abs(distance) < 100)
                    return String.Format("{0:N0}m", distance);
                return String.Format("{0:F1}km", distance / 1000.000);
            }
            // <0.1 miles, show yards.
            if (Math.Abs(distance) < 160.9344)
                return String.Format("{0:N0}yd", distance * 1.093613298337708);
            return String.Format("{0:F1}mi", distance / 1609.344);
        }
    }

    public class TrackMonitor : Control
    {
        static Texture2D MonitorTexture;
        WindowTextFont Font;

        public float Heading;
        
        private int MAXDISTANCE;
        private int TRACKMONITORHEIGHT;
        private int LABELSHEIGHT;
        private int fontOffset = 0;
        private bool milepostUnitsMetric;
        public bool OverSpeed = false;
        private bool OLSignalShown = false;
        public float OLSignalDistance = 0f;
        private float lastObjectDistnace = 5000f;
        public TrackMonitorSignalAspect OLSignalAspect; // Out of limit signal

        private Texture2D trainIcon;
        private Texture2D SignalAspects;

        public List<ObjectItemInfo> trackObjects;

        static readonly Dictionary<TrackMonitorSignalAspect, Rectangle> SignalAspectSources = InitSignalAspectSources();
        static Dictionary<TrackMonitorSignalAspect, Rectangle> InitSignalAspectSources()
        {
            return new Dictionary<TrackMonitorSignalAspect, Rectangle> {
                                { TrackMonitorSignalAspect.None, new Rectangle(0, 0, 16, 16) },
                                { TrackMonitorSignalAspect.Clear, new Rectangle(16, 0, 16, 16) },
                                { TrackMonitorSignalAspect.Warning, new Rectangle(0, 16, 16, 16) },
                                { TrackMonitorSignalAspect.Stop, new Rectangle(16, 16, 16, 16) },
                        };
        }

        public TrackMonitor(int width, int height, int maxDistance, int trackMonitorHeight, int labelsHeght, bool metricUnits, Texture2D train, Texture2D signalAspects,
            List<ObjectItemInfo> listOfTrackObjects)
            : base(0, 0, width, height)
        {
            MAXDISTANCE = maxDistance;
            TRACKMONITORHEIGHT = trackMonitorHeight;
            LABELSHEIGHT = labelsHeght;
            milepostUnitsMetric = metricUnits;
            trackObjects = listOfTrackObjects;
            trainIcon = train;
            SignalAspects = signalAspects;
        }

        public override void Initialize(WindowManager windowManager)
        {
            base.Initialize(windowManager);
            Font = windowManager.TextManager.Get("Arial", 7, System.Drawing.FontStyle.Regular);
        }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            if (MonitorTexture == null)
            {
                MonitorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
                MonitorTexture.SetData(new[] { Color.White });
            }

            drawTrack(spriteBatch, offset, LABELSHEIGHT, TRACKMONITORHEIGHT, (OverSpeed) ? Color.Red : Color.Green);
            spriteBatch.Draw(trainIcon, new Rectangle(offset.X + 41, offset.Y + LABELSHEIGHT + TRACKMONITORHEIGHT, trainIcon.Width, trainIcon.Height), Color.White);

            if (!arrayContainsVisibleSignal())
            {
                if (OLSignalDistance > (float)MAXDISTANCE)
                {
                    spriteBatch.Draw(SignalAspects, new Rectangle(offset.X + 130, offset.Y + LABELSHEIGHT + 3, 16, 16), SignalAspectSources[OLSignalAspect], Color.White);
                    spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + LABELSHEIGHT + 5 + 16), null, Color.White, 0, Vector2.Zero,
                        new Vector2(142, 1), SpriteEffects.None, 0);
                    Font.Draw(spriteBatch, new Point(offset.X + 4, offset.Y + LABELSHEIGHT + Font.Height - 4),
                        FormatDistance(OLSignalDistance, milepostUnitsMetric), Color.White);

                    OLSignalShown = true;
                }
            }

            for (int icount = 0; icount < trackObjects.Count; icount++)
            {
                ObjectItemInfo currentObject = trackObjects[icount];
                ObjectItemInfo nextObject = (icount < trackObjects.Count - 2) ? trackObjects[icount + 1] : null;

                float objectDistance = currentObject.distance_to_train;

                if ((offset.Y + LABELSHEIGHT + TRACKMONITORHEIGHT - Convert.ToInt32(objectDistance / (MAXDISTANCE / TRACKMONITORHEIGHT))) > offset.Y + LABELSHEIGHT + 16 + 3
                    + ((OLSignalShown) ? Font.Height + 3 : 0))
                {
                    if (nextObject != null && Math.Abs(nextObject.distance_to_train - objectDistance) < Font.Height + 10)
                    {
                        fontOffset = Font.Height;
                    }
                    else
                    {
                        fontOffset = 0;
                    }

                    if (currentObject.ObjectDetails.isSignal)
                    {
                        var thisAspect = currentObject.ObjectDetails.TranslateTMAspect(currentObject.signal_state);
                        float thisSpeed = currentObject.actual_speed;
                        if (thisSpeed > 0)
                        {
                            drawSignal(spriteBatch, offset, Convert.ToInt32(objectDistance), SignalAspectSources[thisAspect], FormatSpeed(thisSpeed, milepostUnitsMetric), Color.White);
                        }
                        else
                        {
                            drawSignal(spriteBatch, offset, Convert.ToInt32(objectDistance), SignalAspectSources[thisAspect], "", Color.White);
                        }

                        drawDistance(spriteBatch, offset, Convert.ToInt32(objectDistance), FormatDistance(objectDistance, milepostUnitsMetric), Color.White);
                    }
                    else
                    {
                        float thisSpeed = currentObject.actual_speed;
                        if (thisSpeed > 0)
                        {
                            drawSpeed(spriteBatch, offset, Convert.ToInt32(objectDistance), FormatSpeed(thisSpeed, milepostUnitsMetric), Color.White);
                            drawDistance(spriteBatch, offset, Convert.ToInt32(objectDistance), FormatDistance(objectDistance, milepostUnitsMetric), Color.White);
                        }
                    }

                    lastObjectDistnace = objectDistance;
                }
            }
            lastObjectDistnace = 5000;
        }

        private void drawTrack(SpriteBatch spriteBatch, Point offset, float lineStart, float trackDistance, Color lineColor)
        {
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 45, offset.Y + lineStart + 2), null, lineColor, 0, Vector2.Zero,
                new Vector2(2, trackDistance), SpriteEffects.None, 0);
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 55, offset.Y + lineStart + 2), null, lineColor, 0, Vector2.Zero,
                new Vector2(2, trackDistance), SpriteEffects.None, 0);
        }

        private void drawDistance(SpriteBatch spriteBatch, Point offset, float distance, string distanceFromTrain, Color textColor)
        {
            spriteBatch.Draw(MonitorTexture, new Vector2(offset.X + 4, offset.Y + LABELSHEIGHT + TRACKMONITORHEIGHT - Convert.ToInt32(distance / (MAXDISTANCE / TRACKMONITORHEIGHT))),
                null, Color.White, 0, Vector2.Zero, new Vector2(142, 1), SpriteEffects.None, 0);
            Font.Draw(spriteBatch, new Point(offset.X + 4, offset.Y + LABELSHEIGHT + TRACKMONITORHEIGHT + fontOffset - Convert.ToInt32(distance / (MAXDISTANCE / TRACKMONITORHEIGHT))
                - Font.Height), distanceFromTrain, textColor);
        }

        private void drawSpeed(SpriteBatch spriteBatch, Point offset, float speedDistance, string speedLimit, Color textColor)
        {
            Font.Draw(spriteBatch, new Point(offset.X + 60, offset.Y + LABELSHEIGHT + TRACKMONITORHEIGHT + fontOffset - Convert.ToInt32(speedDistance / (MAXDISTANCE / TRACKMONITORHEIGHT))
                - Font.Height), speedLimit, textColor);
        }

        private void drawSignal(SpriteBatch spriteBatch, Point offset, float signalDistance, Rectangle signalAspect, string speedLimit, Color textColor)
        {
            spriteBatch.Draw(SignalAspects, new Rectangle(offset.X + 130, offset.Y + LABELSHEIGHT + TRACKMONITORHEIGHT + fontOffset
                - Convert.ToInt32(signalDistance / (MAXDISTANCE / TRACKMONITORHEIGHT)) - 18, 16, 16), signalAspect, Color.White);

            if (!string.IsNullOrEmpty(speedLimit))
            {
                Font.Draw(spriteBatch, new Point(offset.X + 60, offset.Y + LABELSHEIGHT + TRACKMONITORHEIGHT + fontOffset
                    - Convert.ToInt32(signalDistance / (MAXDISTANCE / TRACKMONITORHEIGHT)) - Font.Height), speedLimit, textColor);
            }
        }

        private bool arrayContainsVisibleSignal()
        {
            bool contains = false;

            foreach (ObjectItemInfo o in trackObjects)
            {
                if (o.ObjectDetails.isSignal && o.distance_to_train < 5000f)
                {
                    contains = true;
                    break;
                }
            }

            return contains;
        }

        public static string FormatSpeed(float speed, bool metric)
        {
            return String.Format(metric ? "{0:F1}kph" : "{0:F1}mph", MpS.FromMpS(speed, metric));
        }

        public static string FormatDistance(float distance, bool metric)
        {
            if (metric)
            {   
                // <0.1 kilometers, show meters.
                if (Math.Abs(distance) < 100)
                    return String.Format("{0:N0}m", distance);
                return String.Format("{0:F1}km", distance / 1000.000);
            }
            // <0.1 miles, show yards.
            if (Math.Abs(distance) < 160.9344)
                return String.Format("{0:N0}yd", distance * 1.093613298337708);
            return String.Format("{0:F1}mi", distance / 1609.344);
        }
    }
}

