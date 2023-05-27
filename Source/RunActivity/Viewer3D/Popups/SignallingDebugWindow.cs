// COPYRIGHT 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.Signalling;
using ORTS.Common;

namespace Orts.Viewer3D.Popups
{
    [CallOnThread("Updater")]
    public class SignallingDebugWindow : LayeredWindow
    {

        public enum DebugWindowSignalAspect
        {
            Clear,
            Warning,
            Stop,
        }

        const float SignalErrorDistance = 100;
        const float SignalWarningDistance = 500;
        const float DisplayDistance = 1000;
        const float DisplaySegmentLength = 10;
        const float MaximumSectionDistance = 10000;

        Viewport Viewport;
        Dictionary<int, TrackSectionCacheEntry> Cache = new Dictionary<int, TrackSectionCacheEntry>();
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

                foreach (var cache in Cache.Values)
                    cache.Age++;

                foreach (var train in Owner.Viewer.Simulator.Trains)
                {
                    var position = train.MUDirection != Direction.Reverse ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
                    var caches = new List<TrackSectionCacheEntry>();
                    // Work backwards until we end up on a different track section.
                    var cacheNode = new Traveller(position);
                    cacheNode.ReverseDirection();
                    var initialNodeOffsetCount = 0;
                    while (cacheNode.TrackNodeIndex == position.TrackNodeIndex && cacheNode.NextSection())
                        initialNodeOffsetCount++;
                    // Now do it again, but don't go the last track section (because it is from a different track node).
                    cacheNode = new Traveller(position);
                    cacheNode.ReverseDirection();
                    for (var i = 1; i < initialNodeOffsetCount; i++)
                        cacheNode.NextSection();
                    // Push the location right up to the end of the section.
                    cacheNode.MoveInSection(MaximumSectionDistance);
                    // Now back facing the right way, calculate the distance to the train location.
                    cacheNode.ReverseDirection();
                    var initialNodeOffset = cacheNode.DistanceTo(position.TileX, position.TileZ, position.X, position.Y, position.Z);
                    // Go and collect all the cache entries for the visible range of vector nodes (straights, curves).
                    var totalDistance = 0f;
                    while (!cacheNode.IsEnd && totalDistance - initialNodeOffset < DisplayDistance)
                    {
                        if (cacheNode.IsTrack)
                        {
                            var cache = GetCacheEntry(cacheNode);
                            cache.Age = 0;
                            caches.Add(cache);
                            totalDistance += cache.Length;
                        }
                        var nodeIndex = cacheNode.TrackNodeIndex;
                        while (cacheNode.TrackNodeIndex == nodeIndex && cacheNode.NextSection()) ;
                    }

                    var switchErrorDistance = initialNodeOffset + DisplayDistance + SignalWarningDistance;
                    var signalErrorDistance = initialNodeOffset + DisplayDistance + SignalWarningDistance;
                    var currentDistance = 0f;
                    foreach (var cache in caches)
                    {
                        foreach (var obj in cache.Objects)
                        {
                            var objDistance = currentDistance + obj.Distance;
                            if (objDistance < initialNodeOffset)
                                continue;

                            var switchObj = obj as TrackSectionSwitch;
                            var signalObj = obj as TrackSectionSignal;
                            if (switchObj != null)
                            {
                                for (var pin = switchObj.TrackNode.Inpins; pin < switchObj.TrackNode.Inpins + switchObj.TrackNode.Outpins; pin++)
                                {
                                    if (switchObj.TrackNode.TrPins[pin].Link == switchObj.NodeIndex)
                                    {
                                        if (pin - switchObj.TrackNode.Inpins != switchObj.TrackNode.TrJunctionNode.SelectedRoute)
                                            switchErrorDistance = objDistance;
                                        break;
                                    }
                                }
                                if (switchErrorDistance < DisplayDistance)
                                    break;
                            }
                            else if (signalObj != null)
                            {
                                if (GetAspect(signalObj.Signal) == DebugWindowSignalAspect.Stop)
                                {
                                    signalErrorDistance = objDistance;
                                    break;
                                }
                            }
                        }
                        if (switchErrorDistance < DisplayDistance || signalErrorDistance < DisplayDistance)
                            break;
                        currentDistance += cache.Length;
                    }

                    var currentPosition = new Traveller(position);
                    currentPosition.Move(-initialNodeOffset);
                    currentDistance = 0;
                    foreach (var cache in caches)
                    {
                        var lastObjDistance = 0f;
                        foreach (var obj in cache.Objects)
                        {
                            var objDistance = currentDistance + obj.Distance;

                            for (var step = lastObjDistance; step < obj.Distance; step += DisplaySegmentLength)
                            {
                                var stepDistance = currentDistance + step;
                                var stepLength = DisplaySegmentLength > obj.Distance - step ? obj.Distance - step : DisplaySegmentLength;
                                var previousLocation = currentPosition.WorldLocation;
                                currentPosition.Move(stepLength);
                                if (stepDistance + stepLength >= initialNodeOffset && stepDistance <= initialNodeOffset + DisplayDistance)
                                    primitives.Add(new DispatcherLineSegment(previousLocation, currentPosition.WorldLocation, signalErrorDistance - stepDistance < SignalErrorDistance ? Color.Red : signalErrorDistance - stepDistance < SignalWarningDistance ? Color.Yellow : Color.White, 2));
                            }
                            lastObjDistance = obj.Distance;

                            if (objDistance >= switchErrorDistance || objDistance >= signalErrorDistance)
                                break;
                        }
                        currentDistance += cache.Length;
                        if (currentDistance >= switchErrorDistance || currentDistance >= signalErrorDistance)
                            break;
                    }

                    currentPosition = new Traveller(position);
                    currentPosition.Move(-initialNodeOffset);
                    currentDistance = 0;
                    foreach (var cache in caches)
                    {
                        var lastObjDistance = 0f;
                        foreach (var obj in cache.Objects)
                        {
                            currentPosition.Move(obj.Distance - lastObjDistance);
                            lastObjDistance = obj.Distance;

                            var objDistance = currentDistance + obj.Distance;
                            if (objDistance < initialNodeOffset || objDistance > initialNodeOffset + DisplayDistance)
                                continue;

                            var eolObj = obj as TrackSectionEndOfLine;
                            var switchObj = obj as TrackSectionSwitch;
                            var signalObj = obj as TrackSectionSignal;
                            if (eolObj != null)
                            {
                                primitives.Add(new DispatcherLabel(currentPosition.WorldLocation, Color.Red, "End of Line", Owner.TextFontDefaultOutlined));
                            }
                            else if (switchObj != null)
                            {
                                primitives.Add(new DispatcherLabel(currentPosition.WorldLocation, objDistance >= switchErrorDistance ? Color.Red : Color.White, String.Format("Switch ({0}, {1}-way, {2} set)", switchObj.TrackNode.Index, switchObj.TrackNode.Outpins, switchObj.TrackNode.TrJunctionNode.SelectedRoute + 1), Owner.TextFontDefaultOutlined));
                            }
                            else if (signalObj != null)
                            {
                                var aspects = string.Join(" / ", signalObj.Signal.SignalHeads.Select(
                                    head => $"{head.state}" + (head.TextSignalAspect.Length > 0 ? $" ({head.TextSignalAspect})" : string.Empty)
                                    ));
                                if (signalObj.Signal.Type == SignalObjectType.SpeedSignal)
                                {
                                    string speedText = "None / None";
                                    if (signalObj.Signal.SignalHeads[0].CurrentSpeedInfo is ObjectSpeedInfo speedInfoItem)
                                    {
                                        speedText = $"{FormatStrings.FormatSpeed(speedInfoItem.speed_pass, true)} / {FormatStrings.FormatSpeed(speedInfoItem.speed_freight, true)}";
                                    }

                                    primitives.Add(new DispatcherLabel(currentPosition.WorldLocation,
                                               GetAspect(signalObj.Signal) == DebugWindowSignalAspect.Stop ? Color.Red :
                                                   GetAspect(signalObj.Signal) == DebugWindowSignalAspect.Warning ? Color.Yellow :
                                                   Color.Green,
                                            $"Speed signal {signalObj.Signal.thisRef} / {speedText} / ({aspects})",
                                            Owner.TextFontDefaultOutlined));
                                }
                                else
                                {
                                    primitives.Add(new DispatcherLabel(currentPosition.WorldLocation,
                                               GetAspect(signalObj.Signal) == DebugWindowSignalAspect.Stop ? Color.Red :
                                                   GetAspect(signalObj.Signal) == DebugWindowSignalAspect.Warning ? Color.Yellow :
                                                   Color.Green,
                                               $"Signal {signalObj.Signal.thisRef} ({aspects})",
                                               Owner.TextFontDefaultOutlined));
                                }
                            }

                            if (objDistance >= switchErrorDistance || objDistance >= signalErrorDistance)
                                break;
                        }
                        currentDistance += cache.Length;
                        if (currentDistance >= switchErrorDistance || currentDistance >= signalErrorDistance)
                            break;
                    }
                }
                Primitives = primitives;

                // Clean up any cache entries who haven't been using for 30 seconds.
                var oldCaches = Cache.Where(kvp => kvp.Value.Age > 30 * 4).ToArray();
                foreach (var oldCache in oldCaches)
                    Cache.Remove(oldCache.Key);
            }

            var labels = new List<Rectangle>();
            foreach (var primitive in Primitives)
                primitive.PrepareFrame(labels, Viewport, Owner.Viewer.Camera);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            foreach (var line in Primitives)
                line.Draw(spriteBatch);
        }

        TrackSectionCacheEntry GetCacheEntry(Traveller position)
        {
            TrackSectionCacheEntry rv;
            if (Cache.TryGetValue(position.TrackNodeIndex, out rv) && (rv.Direction == position.Direction))
                return rv;
            Cache[position.TrackNodeIndex] = rv = new TrackSectionCacheEntry()
            {
                Direction = position.Direction,
                Length = 0,
                Objects = new List<TrackSectionObject>(),
            };
            var nodeIndex = position.TrackNodeIndex;
            var trackNode = new Traveller(position);
            while (true)
            {
                rv.Length += MaximumSectionDistance - trackNode.MoveInSection(MaximumSectionDistance);
                if (!trackNode.NextSection())
                    break;
                if (trackNode.IsEnd)
                    rv.Objects.Add(new TrackSectionEndOfLine() { Distance = rv.Length });
                else if (trackNode.IsJunction)
                    rv.Objects.Add(new TrackSectionSwitch() { Distance = rv.Length, TrackNode = trackNode.TN, NodeIndex = nodeIndex });
                else
                    rv.Objects.Add(new TrackSectionObject() { Distance = rv.Length }); // Always have an object at the end.
                if (trackNode.TrackNodeIndex != nodeIndex)
                    break;
            }
            trackNode = new Traveller(position);
            var distance = 0f;
            while (true)
            {
                Train.TCPosition thisPosition = new Train.TCPosition();
                TrackNode tn = trackNode.TN;
                float offset = trackNode.TrackNodeOffset;
                int direction = (int)trackNode.Direction;

                thisPosition.SetTCPosition(tn.TCCrossReference, offset, direction);
                Train.TCSubpathRoute tempRoute = Owner.Viewer.Simulator.Signals.BuildTempRoute(null, thisPosition.TCSectionIndex, thisPosition.TCOffset, thisPosition.TCDirection, 5000.0f, true, false, false);

                ObjectItemInfo thisInfo = Owner.Viewer.Simulator.Signals.GetNextSignal_InRoute(null, tempRoute, 0, thisPosition.TCOffset, -1, thisPosition, null);

                var signal = thisInfo.ObjectDetails;
                if (signal == null)
                    break;
                var signalDistance = thisInfo.distance_found;

                if (signalDistance > 0)
                {
                    var oldDistance = distance;
                    distance += signalDistance;
                    if (distance - oldDistance <= 0.001 || distance >= 10000)
                        break;
                    trackNode.Move(signalDistance);
                    if (trackNode.TrackNodeIndex != nodeIndex)
                        break;
                    rv.Objects.Add(new TrackSectionSignal() { Distance = distance, Signal = signal });
                }
                else
                {
                    if ((rv.Objects.Last() as TrackSectionSignal).Signal == signal)
                    {
                        Trace.TraceInformation("Exit from signal search loop");
                        break;
                    }
                }
            }
            rv.Objects = rv.Objects.OrderBy(tso => tso.Distance).ToList();
            return rv;
        }

        static DebugWindowSignalAspect GetAspect(SignalObject signal)
        {
            var aspect = signal.this_sig_lr(MstsSignalFunction.NORMAL);

            if (aspect >= MstsSignalAspect.CLEAR_1)
                return DebugWindowSignalAspect.Clear;
            if (aspect >= MstsSignalAspect.STOP_AND_PROCEED)
                return DebugWindowSignalAspect.Warning;
            return DebugWindowSignalAspect.Stop;
        }

        enum DistanceToType
        {
            Nothing,
            EndOfLine,
            Switch,
            Signal,
        }

        public class TrackSectionCacheEntry
        {
            public int Age;
            public Traveller.TravellerDirection Direction;
            public float Length;
            public List<TrackSectionObject> Objects;
        }

        public class TrackSectionObject
        {
            public float Distance;
        }

        public class TrackSectionEndOfLine : TrackSectionObject
        {
        }

        public class TrackSectionSwitch : TrackSectionObject
        {
            public TrackNode TrackNode;
            public int NodeIndex;
        }

        public class TrackSectionSignal : TrackSectionObject
        {
            public SignalObject Signal;
        }
    }

    [CallOnThread("Updater")]
    public abstract class DispatcherPrimitive
    {
        protected static Vector3 Normalize(WorldLocation location, Orts.Viewer3D.Camera camera)
        {
            return new Vector3(location.Location.X + (location.TileX - camera.TileX) * 2048, location.Location.Y, -location.Location.Z - (location.TileZ - camera.TileZ) * 2048);
        }

        protected static Vector3 Project3D(Vector3 position, Viewport viewport, Orts.Viewer3D.Camera camera)
        {
            return viewport.Project(position, camera.XnaProjection, camera.XnaView, Matrix.Identity);
        }

        protected static Vector2 Flatten(Vector3 position)
        {
            return new Vector2(position.X, position.Y);
        }

        public abstract void PrepareFrame(List<Rectangle> labels, Viewport viewport, Orts.Viewer3D.Camera camera);

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

        public override void PrepareFrame(List<Rectangle> labels, Viewport viewport, Orts.Viewer3D.Camera camera)
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

        public override void PrepareFrame(List<Rectangle> labels, Viewport viewport, Orts.Viewer3D.Camera camera)
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
