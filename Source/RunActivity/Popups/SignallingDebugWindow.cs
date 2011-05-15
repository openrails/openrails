// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

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

                    var position1 = position.WorldLocation;
                    var distance = 0f;
                    var distanceToEnd = -1f;
                    var distanceToSwitch = -1f;
                    TrackNode switchTrackNode = null;
                    bool switchAgainst = false;
                    var distanceToSignal = -1f;
                    Signal signalNode = null;
                    do
                    {
                        if (distanceToEnd < 0)
                        {
                            distanceToEnd = 0;
                            var nextSection = new TDBTraveller(position);
                            while (true)
                            {
                                distanceToEnd += MaximumSectionDistance - nextSection.MoveInSection(MaximumSectionDistance);
                                if (!nextSection.NextSection() || nextSection.TN.TrEndNode || distanceToEnd > DisplayDistance + DisplaySegmentLength)
                                    break;
                            }
                        }
                        if (distanceToSwitch < 0)
                        {
                            distanceToSwitch = 0;
                            var nextSection = new TDBTraveller(position);
                            var lastSectionID = nextSection.TrackNodeIndex;
                            while (true)
                            {
                                distanceToSwitch += MaximumSectionDistance - nextSection.MoveInSection(MaximumSectionDistance);
                                if (!nextSection.NextSection() || nextSection.TN.TrEndNode || distanceToEnd > DisplayDistance || nextSection.TN.TrJunctionNode != null)
                                    break;
                                lastSectionID = nextSection.TrackNodeIndex;
                            }
                            if (nextSection.TN == null || nextSection.TN.TrJunctionNode == null)
                            {
                                distanceToSwitch = -1;
                            }
                            else
                            {
                                switchTrackNode = nextSection.TN;
                                switchAgainst = false;
                                for (var pin = switchTrackNode.Inpins; pin < switchTrackNode.Inpins + switchTrackNode.Outpins; pin++)
                                {
                                    if (switchTrackNode.TrPins[pin].Link == lastSectionID)
                                    {
                                        switchAgainst = (pin - switchTrackNode.Inpins) != switchTrackNode.TrJunctionNode.SelectedRoute;
                                        break;
                                    }
                                }
                            }
                        }
                        if (distanceToSignal < 0)
                        {
                            if (Owner.Viewer.Simulator.Signals.FindNextSignal(position) != -1)
                            {
                                signalNode = Owner.Viewer.Simulator.Signals.FindNearestSignal(position);
                                distanceToSignal = signalNode.DistanceToSignal(position);
                            }
                        }

                        var distanceTravelled = DisplaySegmentLength;
                        if (distanceToEnd > 0 && distanceToEnd < distanceTravelled)
                            distanceTravelled = distanceToEnd;
                        if (distanceToSwitch > 0 && distanceToSwitch < distanceTravelled)
                            distanceTravelled = distanceToSwitch;
                        if (distanceToSignal > 0 && distanceToSignal < distanceTravelled)
                            distanceTravelled = distanceToSignal;

                        position.Move(distanceTravelled);

                        var switchError = distanceToSwitch > 0 && switchAgainst;
                        var signalAspectStop = distanceToSignal > 0 && signalNode.GetMonitorAspect() == TrackMonitorSignalAspect.Stop;
                        var signalAspectWarning = distanceToSignal > 0 && signalNode.GetMonitorAspect() == TrackMonitorSignalAspect.Warning;
                        var signalWarning = signalAspectStop && distanceToSignal < 500;
                        var signalError = signalAspectStop && distanceToSignal < 100;

                        var position2 = position.WorldLocation;
                        primitives.Add(new DispatcherLineSegment(position1, position2, signalError ? Color.Red : signalWarning ? Color.Yellow : Color.White, 2));
                        if (distanceTravelled == distanceToEnd)
                            primitives.Add(new DispatcherLabel(position2, Color.Red, "End of Line"));
                        if (distanceTravelled == distanceToSwitch)
                            primitives.Add(new DispatcherLabel(position2, switchError ? Color.Red : Color.White, String.Format("Switch ({0}-way, {1} set)", switchTrackNode.Outpins, switchTrackNode.TrJunctionNode.SelectedRoute + 1)));
                        if (distanceTravelled == distanceToSignal)
                            primitives.Add(new DispatcherLabel(position2, signalAspectStop ? Color.Red : signalAspectWarning ? Color.Yellow : Color.White, String.Format("Signal ({0})", signalNode.GetAspect())));

                        if (distanceTravelled == distanceToEnd)
                            break;
                        if (distanceTravelled == distanceToSwitch && switchError)
                            break;
                        if (distanceTravelled == distanceToSignal && signalAspectStop)
                            break;

                        distance += distanceTravelled;
                        distanceToEnd -= distanceTravelled;
                        distanceToSwitch -= distanceTravelled;
                        distanceToSignal -= distanceTravelled;
                        position1 = position2;
                    }
                    while (distance < DisplayDistance);
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

            Visible = (start2d.Z >= 0 && start2d.Z <= 1 && end2d.Z >= 0 && end2d.Z <= 1);
            Start2D = Flatten(start2d);
            Angle = (float)Math.Atan2(end2d.Y - start2d.Y, end2d.X - start2d.X);
            Length = (end2d - start2d).Length();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Visible)
            {
                spriteBatch.Draw(WindowManager.WhiteTexture, Start2D, null, Color, Angle, Vector2.Zero, new Vector2(Length, Width), SpriteEffects.None, 0);
                //spriteBatch.Draw(WindowManager.WhiteTexture, Start2D, null, Color.Red, Angle, Vector2.Zero, new Vector2(Length / 2, Width), SpriteEffects.None, 0);
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
        Vector2 TextSize;

        bool Visible;
        float LabelOffset;
        Vector2 Position2D;

        public DispatcherLabel(WorldLocation position, Color color, string text)
        {
            Position = position;
            Color = color;
            Text = text;
            TextSize = Materials.SpriteBatchMaterial.DefaultFont.MeasureString(text);
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
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Visible)
            {
                spriteBatch.Draw(WindowManager.WhiteTexture, Position2D, null, Color, 0, Vector2.Zero, new Vector2(1, LabelOffset), SpriteEffects.None, 0);
                spriteBatch.DrawString(Materials.SpriteBatchMaterial.DefaultFont, Text, new Vector2(Position2D.X + TextOffsetX, Position2D.Y + TextOffsetY), Color);
            }
        }
    }
}
