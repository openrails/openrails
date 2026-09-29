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
            List<DynamicTrackViewer> output, RulerObj ruler,
            WorldPosition tilePosition)
        {
            if (ruler.RulerPoints == null || ruler.RulerPoints.Count < 2)
                return;

            TrProfile profile;
            if (!TRPFile.TryResolveRulerProfile(
                    viewer.TRPs, ruler.ShapeTemplate, out profile))
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
                if (spanDirection.LengthSquared() > 0)
                    spanDirection.Normalize();
                spanDirections.Add(spanDirection);
            }
            var nodeDirections = new List<Vector3>();
            for (int i = 0; i < points.Count; i++)
                nodeDirections.Add(GetNodeDirection(spanDirections, i));

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
                Vector3 up = Vector3.Up;
                if (Math.Abs(Vector3.Dot(direction, up)) > 0.999f)
                    up = Vector3.Forward;

                // Procedural vertices remain local to this segment. This
                // matrix is supplied later as the normal render transform.
                Matrix segmentWorld = Matrix.CreateWorld(
                    tileStart, direction, up);

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
            if (nodeIndex <= 0)
                return spanDirections[0];
            if (nodeIndex >= spanDirections.Count)
                return spanDirections[spanDirections.Count - 1];

            Vector3 direction = spanDirections[nodeIndex - 1] +
                spanDirections[nodeIndex];
            if (direction.LengthSquared() < 0.000001f)
                return spanDirections[nodeIndex];
            direction.Normalize();
            return direction;
        }
    }
}
