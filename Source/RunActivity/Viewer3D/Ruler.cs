// COPYRIGHT 2026 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using ORTS.Common;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Converts TSRE Ruler point pairs into straight procedural profile
    /// sections. Rulers are scenery only and do not use TDB or RDB data.
    /// </summary>
    public static class RulerShape
    {
        const float MinimumSegmentLength = 0.001f;

        public static void Decompose(Viewer viewer,
            List<DynamicTrackViewer> output,
            List<StaticShape> sceneryObjects, RulerObj ruler,
            WorldPosition tilePosition, string nodeShapePath,
            ShapeFlags nodeShapeFlags)
        {
            if (ruler.RulerPoints == null || ruler.RulerPoints.Count < 2)
                return;

            var points = new List<Vector3>();
            foreach (point rulerPoint in ruler.RulerPoints)
                points.Add(new Vector3(rulerPoint.X, rulerPoint.Y,
                    -rulerPoint.Z));
            if (points.Count < 2)
                return;
            var spanDirections = new List<Vector3>();
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 spanDirection = points[i + 1] - points[i];
                if (spanDirection.LengthSquared() >=
                    MinimumSegmentLength * MinimumSegmentLength)
                    spanDirection.Normalize();
                else
                    spanDirection = Vector3.Zero;
                spanDirections.Add(spanDirection);
            }
            var nodeDirections = new List<Vector3>();
            for (int i = 0; i < points.Count; i++)
                nodeDirections.Add(GetNodeDirection(spanDirections, i));

            // FileName is an optional ordinary MSTS shape placed at every
            // authored Ruler node. It is independent of ShapeTemplate so a
            // Ruler can use node shapes without procedural span geometry.
            if (!String.IsNullOrEmpty(nodeShapePath))
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var nodePosition = new WorldPosition
                    {
                        TileX = tilePosition.TileX,
                        TileZ = tilePosition.TileZ,
                        XNAMatrix = CreatePathMatrix(points[i],
                            nodeDirections[i]),
                    };
                    sceneryObjects.Add(new StaticShape(viewer,
                        nodeShapePath, nodePosition, nodeShapeFlags));
                }
            }

            TrProfile profile;
            if (!TRPFile.TryResolveRulerProfile(
                    viewer.TRPs, ruler.ShapeTemplate, out profile))
                return;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                // Unlike normal world objects, TSRE Ruler points are already
                // tile-relative positions; Position normally repeats the
                // first point. Convert Z once, but do not apply Position or
                // QDirection again.
                Vector3 tileStart = points[i];
                Vector3 tileEnd = points[i + 1];
                Vector3 direction = tileEnd - tileStart;
                float length = direction.Length();
                if (length < MinimumSegmentLength)
                    continue;

                direction /= length;

                // Procedural vertices remain local to this segment. This
                // matrix is supplied later as the normal render transform.
                Matrix segmentWorld = CreatePathMatrix(tileStart, direction);

                var start = new WorldPosition
                {
                    TileX = tilePosition.TileX,
                    TileZ = tilePosition.TileZ,
                    XNAMatrix = segmentWorld
                };
                var end = new WorldPosition(start);
                end.XNAMatrix.Translation = tileEnd;

                // DynamicTrackViewer stores only the start tile. Keep the
                // endpoint expressed relative to that same tile for culling.
                start.Normalize();
                end.NormalizeTo(start.TileX, start.TileZ);

                var pathContext = new TrackProfilePathContext
                {
                    ObjectIndex = (int)ruler.UID,
                    SpanIndex = i,
                    IsPointPath = true,
                    OwnStartNode = true,
                    OwnEndNode = i + 2 == points.Count,
                    OwnPathStart = i == 0,
                    OwnPathEnd = i + 2 == points.Count,
                    StartDirection = nodeDirections[i],
                    EndDirection = nodeDirections[i + 1],
                };
                output.Add(new SuperElevationViewer(
                    viewer, start, end, -1, length, profile,
                    pathContext: pathContext));
            }
        }

        static Vector3 GetNodeDirection(List<Vector3> spanDirections,
            int nodeIndex)
        {
            Vector3 incoming = Vector3.Zero;
            bool hasIncoming = false;
            for (int i = Math.Min(nodeIndex - 1,
                    spanDirections.Count - 1); i >= 0; i--)
            {
                if (spanDirections[i].LengthSquared() <= 0)
                    continue;
                incoming = spanDirections[i];
                hasIncoming = true;
                break;
            }

            Vector3 outgoing = Vector3.Zero;
            bool hasOutgoing = false;
            for (int i = Math.Max(nodeIndex, 0);
                    i < spanDirections.Count; i++)
            {
                if (spanDirections[i].LengthSquared() <= 0)
                    continue;
                outgoing = spanDirections[i];
                hasOutgoing = true;
                break;
            }

            if (hasIncoming && hasOutgoing)
            {
                Vector3 direction = incoming + outgoing;
                if (direction.LengthSquared() > 0.000001f)
                {
                    direction.Normalize();
                    return direction;
                }
                return outgoing;
            }
            if (hasOutgoing)
                return outgoing;
            if (hasIncoming)
                return incoming;
            return Vector3.Forward;
        }

        static Matrix CreatePathMatrix(Vector3 position, Vector3 direction)
        {
            Vector3 up = Vector3.Up;
            if (Math.Abs(Vector3.Dot(direction, up)) > 0.999f)
                up = Vector3.Forward;
            return Matrix.CreateWorld(position, direction, up);
        }
    }
}
