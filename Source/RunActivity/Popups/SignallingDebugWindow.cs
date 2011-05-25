// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;
using ORTS.Popups;

namespace ORTS
{
    [CallOnThread("Updater")]
    public class SignallingDebugWindow : LayeredWindow
    {
        const float DisplayDistance = 1000;
        const float DisplaySegmentLength = 10;
        const float MaximumSectionDistance = 10000;

        Viewport Viewport;
        List<DispatcherPrimitive> Primitives = new List<DispatcherPrimitive>();

        public SignallingDebugWindow(WindowManager owner)
            : base(owner, 1, 1, "Dispatcher Debug")
        {
        }

        internal override void ScreenChanged()
        {
            base.ScreenChanged();
            Viewport = Owner.Viewer.RenderProcess.GraphicsDevice.Viewport;
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var primitives = new List<DispatcherPrimitive>(Primitives.Count);
                var camera = Owner.Viewer.Camera;
                Func<WorldLocation, Vector3> Normalize = (location) => new Vector3(location.Location.X + (location.TileX - camera.TileX) * 2048, location.Location.Y, -location.Location.Z - (location.TileZ - camera.TileZ) * 2048);
                Func<Vector3, Vector3> Project3D = (position) => Viewport.Project(position, camera.XNAProjection, camera.XNAView, Matrix.Identity);
                Func<Vector3, Vector2> Flatten = (position) => new Vector2(position.X, position.Y);

                foreach (var train in Owner.Viewer.Simulator.Trains)
                {
                    var position = new TDBTraveller(train.MUDirection == Direction.Forward ? train.FrontTDBTraveller : train.RearTDBTraveller);
                    if (train.MUDirection == Direction.Reverse)
                        position.ReverseDirection();

                    var distanceTravelled = 0f;

                    var distanceToEnd = 0f;
                    {
                        var nextSection = new TDBTraveller(position);
                        while (true)
                        {
                            distanceToEnd += MaximumSectionDistance - nextSection.MoveInSection(MaximumSectionDistance);
                            if (!nextSection.NextSection() || nextSection.TN.TrEndNode || distanceToEnd > DisplayDistance + DisplaySegmentLength)
                                break;
                        }
                    }

                    var distanceToSwitch = new List<DistanceToSwitch>();
                    {
                        var nextSection = new TDBTraveller(position);
                        var nextDistance = 0f;
                        var lastSectionID = nextSection.TrackNodeIndex;
                        while (true)
                        {
                            nextDistance += MaximumSectionDistance - nextSection.MoveInSection(MaximumSectionDistance);
                            if (!nextSection.NextSection() || nextSection.TN.TrEndNode || nextDistance > DisplayDistance)
                                break;
                            if (nextSection.TN.TrJunctionNode != null)
                                distanceToSwitch.Add(new DistanceToSwitch() { Distance = nextDistance, TN = nextSection.TN, LastSectionID = lastSectionID });
                            lastSectionID = nextSection.TrackNodeIndex;
                        }
                    }

                    var distanceToSignal = new List<DistanceToSignal>();
                    {
                        var nextSection = new TDBTraveller(position);
                        var nextDistance = 0f;
                        while (true)
                        {
                            var signalNode = Owner.Viewer.Simulator.Signals.FindNearestSignal(nextSection);
                            if (signalNode.GetAspect() == SignalHead.SIGASP.UNKNOWN)
                                break;
                            var signalDistance = signalNode.DistanceToSignal(nextSection);
                            nextDistance += signalDistance;
                            distanceToSignal.Add(new DistanceToSignal() { Distance = nextDistance, MonitorAspect = signalNode.GetMonitorAspect(), Aspect = signalNode.GetAspect() });
                            // TODO: This is a massive hack because the current signalling code is useless at finding the next signal in the face of changing switches.
                            nextSection.Move(signalDistance + 0.0001f);
                        }
                    }

                    var position1 = position.WorldLocation;
                    do
                    {
                        var distanceToTravel = DisplaySegmentLength;
                        var distanceType = DistanceToType.Nothing;
                        if (distanceToEnd > 0 && distanceTravelled + distanceToTravel > distanceToEnd)
                        {
                            distanceToTravel = distanceToEnd - distanceTravelled;
                            distanceType = DistanceToType.EndOfLine;
                        }
                        if (distanceToSwitch.Count > 0 && distanceTravelled + distanceToTravel > distanceToSwitch[0].Distance)
                        {
                            distanceToTravel = distanceToSwitch[0].Distance - distanceTravelled;
                            distanceType = DistanceToType.Switch;
                        }
                        if (distanceToSignal.Count > 0 && distanceTravelled + distanceToTravel > distanceToSignal[0].Distance)
                        {
                            distanceToTravel = distanceToSignal[0].Distance - distanceTravelled;
                            distanceType = DistanceToType.Signal;
                        }

                        var signalError = false;
                        var signalWarning = false;
                        for (var i = 0; i < distanceToSignal.Count; i++)
                        {
                            if (distanceToSignal[i].MonitorAspect == TrackMonitorSignalAspect.Stop)
                            {
                                signalError |= distanceToSignal[i].Distance - distanceTravelled < 100;
                                signalWarning |= distanceToSignal[i].Distance - distanceTravelled < 500;
                            }
                            if (distanceToSignal[i].Distance - distanceTravelled > 500)
                                break;
                        }

                        position.Move(distanceToTravel);
                        var position2 = position.WorldLocation;
                        primitives.Add(new DispatcherLineSegment(position1, position2, signalError ? Color.Red : signalWarning ? Color.Yellow : Color.White, 2));
                        position1 = position2;
                        distanceTravelled += distanceToTravel;

                        if (distanceType == DistanceToType.EndOfLine)
                        {
                            primitives.Add(new DispatcherLabel(position2, Color.Red, "End of Line", Owner.TextFontDefault));
                            break;
                        }
                        else if (distanceType == DistanceToType.Switch)
                        {
                            var switchError = false;
                            for (var pin = distanceToSwitch[0].TN.Inpins; pin < distanceToSwitch[0].TN.Inpins + distanceToSwitch[0].TN.Outpins; pin++)
                            {
                                if (distanceToSwitch[0].TN.TrPins[pin].Link == distanceToSwitch[0].LastSectionID)
                                {
                                    switchError = (pin - distanceToSwitch[0].TN.Inpins) != distanceToSwitch[0].TN.TrJunctionNode.SelectedRoute;
                                    break;
                                }
                            }
                            primitives.Add(new DispatcherLabel(position2, switchError ? Color.Red : Color.White, String.Format("Switch ({0}-way, {1} set)", distanceToSwitch[0].TN.Outpins, distanceToSwitch[0].TN.TrJunctionNode.SelectedRoute + 1), Owner.TextFontDefault));
                            distanceToSwitch.RemoveAt(0);
                            if (switchError)
                                break;
                        }
                        else if (distanceType == DistanceToType.Signal)
                        {
                            var signalAspectStop = distanceToSignal[0].MonitorAspect == TrackMonitorSignalAspect.Stop;
                            var signalAspectWarning = distanceToSignal[0].MonitorAspect == TrackMonitorSignalAspect.Warning;
                            primitives.Add(new DispatcherLabel(position2, signalAspectStop ? Color.Red : signalAspectWarning ? Color.Yellow : Color.White, String.Format("Signal ({0})", distanceToSignal[0].Aspect), Owner.TextFontDefault));
                            distanceToSignal.RemoveAt(0);
                            if (signalError)
                                break;
                        }
                    }
                    while (distanceTravelled < DisplayDistance);
                }
                Primitives = primitives;
            }

            var labels = new List<Rectangle>();
            foreach (var primitive in Primitives)
                primitive.PrepareFrame(labels, Viewport, Owner.Viewer.Camera);
        }

        [CallOnThread("Render")]
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            foreach (var line in Primitives)
                line.Draw(spriteBatch);
        }

        enum DistanceToType
        {
            Nothing,
            EndOfLine,
            Switch,
            Signal,
        }

        struct DistanceToSwitch
        {
            public float Distance;
            public TrackNode TN;
            public int LastSectionID;
        }

        struct DistanceToSignal
        {
            public float Distance;
            public SignalHead.SIGASP Aspect;
            public TrackMonitorSignalAspect MonitorAspect;
        }
    }

    [CallOnThread("Updater")]
    public abstract class DispatcherPrimitive
    {
        protected Vector3 Normalize(WorldLocation location, Camera camera)
        {
            return new Vector3(location.Location.X + (location.TileX - camera.TileX) * 2048, location.Location.Y, -location.Location.Z - (location.TileZ - camera.TileZ) * 2048);
        }

        protected Vector3 Project3D(Vector3 position, Viewport viewport, Camera camera)
        {
            return viewport.Project(position, camera.XNAProjection, camera.XNAView, Matrix.Identity);
        }

        protected Vector2 Flatten(Vector3 position)
        {
            return new Vector2(position.X, position.Y);
        }

        public abstract void PrepareFrame(List<Rectangle> labels, Viewport viewport, Camera camera);

        [CallOnThread("Render")]
        public abstract void Draw(SpriteBatch spriteBatch);
    }

    [CallOnThread("Updater")]
    public class DispatcherLineSegment : DispatcherPrimitive
    {
        WorldLocation Start;
        WorldLocation End;
        Color Color;
        float Width;

        bool Visible;
        Vector2 Start2D;
        float Angle;
        float Length;

        public DispatcherLineSegment(WorldLocation start, WorldLocation end, Color color, float width)
        {
            Start = start;
            End = end;
            Color = color;
            Width = width;
        }

        public override void PrepareFrame(List<Rectangle> labels, Viewport viewport, Camera camera)
        {
            var start2d = Project3D(Normalize(Start, camera), viewport, camera);
            var end2d = Project3D(Normalize(End, camera), viewport, camera);
            var line2d = end2d - start2d;
            line2d.Normalize();

            Visible = (start2d.Z >= 0 && start2d.Z <= 1 && end2d.Z >= 0 && end2d.Z <= 1);
            Start2D = Flatten(start2d) + new Vector2(line2d.Y * Width / 2, -line2d.X * Width / 2);
            Angle = (float)Math.Atan2(end2d.Y - start2d.Y, end2d.X - start2d.X);
            Length = (end2d - start2d).Length();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Visible)
            {
                spriteBatch.Draw(WindowManager.WhiteTexture, Start2D, null, Color, Angle, Vector2.Zero, new Vector2(Length, Width), SpriteEffects.None, 0);
            }
        }
    }

    [CallOnThread("Updater")]
    public class DispatcherLabel : DispatcherPrimitive
    {
        const int TextOffsetX = 2;
        const int TextOffsetY = -2;

        WorldLocation Position;
        Color Color;
        string Text;
        WindowTextFont Font;
        Vector2 TextSize;

        bool Visible;
        float LabelOffset;
        Vector2 Position2D;
        Point Position2DText;

        public DispatcherLabel(WorldLocation position, Color color, string text, WindowTextFont font)
        {
            Position = position;
            Color = color;
            Text = text;
            Font = font;
            TextSize = new Vector2(Font.MeasureString(text), Font.Height);
        }

        public override void PrepareFrame(List<Rectangle> labels, Viewport viewport, Camera camera)
        {
            var position2D = Project3D(Normalize(Position, camera), viewport, camera);

            Visible = (position2D.Z >= 0 && position2D.Z <= 1);
            if (Visible)
            {
                var rect2D = new Rectangle((int)position2D.X, (int)position2D.Y, (int)TextSize.X + 2 * TextOffsetX, (int)TextSize.Y);
                rect2D.Y -= rect2D.Height;

                while (labels.Any(r => r.Intersects(rect2D)))
                    rect2D.Y = labels.Where(r => r.Intersects(rect2D)).Select(r => r.Top).Max() - rect2D.Height;
                labels.Add(rect2D);

                LabelOffset = position2D.Y - rect2D.Y;
                Position2D = new Vector2(rect2D.X, rect2D.Y);
                Position2DText = new Point((int)Position2D.X + TextOffsetX, (int)Position2D.Y + TextOffsetY);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Visible)
            {
                spriteBatch.Draw(WindowManager.WhiteTexture, Position2D, null, Color, 0, Vector2.Zero, new Vector2(1, LabelOffset), SpriteEffects.None, 0);
                Font.Draw(spriteBatch, Position2DText, Text, Color);
            }
        }
    }
}
