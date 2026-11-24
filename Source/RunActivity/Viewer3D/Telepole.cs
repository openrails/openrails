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
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using ORTS.Common;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Converts a native MSTS Telepole object into instanced pole shapes and
    /// a procedural set of sagging wires.
    /// </summary>
    public static class TelepoleShape
    {
        const int MaximumRenderedPoles = 10000;
        const float WireRadiusM = 0.008f;
        const float SagPerMetre = 0.02f;

        public static void Decompose(Viewer viewer,
            List<DynamicTrackViewer> output,
            List<StaticShape> sceneryObjects, TelepoleObj telepole,
            int tileX, int tileZ, ShapeFlags poleShapeFlags)
        {
            if (!telepole.StartPosition.HasValue ||
                !telepole.EndPosition.HasValue ||
                viewer.TelepoleDataFile == null ||
                telepole.Config >=
                    viewer.TelepoleDataFile.Configurations.Count)
                return;

            TelepoleConfig config = viewer.TelepoleDataFile.Configurations[
                (int)telepole.Config];
            Vector3 start = telepole.StartPosition.Value;
            Vector3 end = telepole.EndPosition.Value;
            int population = GetPopulation(telepole, config, start, end);
            if (population < 2)
                return;
            if (population > MaximumRenderedPoles)
            {
                Trace.TraceWarning(
                    "Telepole object {0} population {1} was limited to {2}",
                    telepole.UID, population, MaximumRenderedPoles);
                population = MaximumRenderedPoles;
            }

            List<Vector3> polePoints = GetGroundedPolePoints(viewer,
                tileX, tileZ, start, end, population);
            AddPoleShapes(viewer, sceneryObjects, polePoints,
                config.ShapeFilePath, tileX, tileZ, poleShapeFlags);

            if (config.Wires.Count == 0)
                return;
            List<Vector3> wirePath = GetWirePath(polePoints);
            if (wirePath.Count < 2)
                return;

            WorldPosition origin = PointWorldPosition(tileX, tileZ,
                wirePath[0]);
            origin.Normalize();
            var localPath = new List<Vector3>(wirePath.Count);
            foreach (Vector3 point in wirePath)
            {
                WorldPosition position = PointWorldPosition(tileX, tileZ,
                    point);
                position.NormalizeTo(origin.TileX, origin.TileZ);
                localPath.Add(position.XNAMatrix.Translation -
                    origin.XNAMatrix.Translation);
            }

            TrProfile profile = CreateWireProfile(viewer, config);
            Vector3 xnaEnd = origin.XNAMatrix.Translation +
                localPath[localPath.Count - 1];
            float radius = GetObjectRadius(localPath,
                origin.XNAMatrix.Translation, xnaEnd, config);
            var primitive = new TelepoleWirePrimitive(viewer,
                localPath, profile, xnaEnd, radius);
            if (primitive.ShapePrimitives.Length == 0)
                return;

            var wireViewer = new DynamicTrackViewer(viewer, origin)
            {
                Primitive = primitive,
            };
            output.Add(wireViewer);
        }

        static int GetPopulation(TelepoleObj telepole,
            TelepoleConfig config, Vector3 start, Vector3 end)
        {
            if (telepole.Population >= 2)
                return telepole.Population > int.MaxValue
                    ? int.MaxValue : (int)telepole.Population;
            if (config.SeparationM <= 0 ||
                float.IsNaN(config.SeparationM) ||
                float.IsInfinity(config.SeparationM))
                return 0;
            return Math.Max(2, (int)Math.Ceiling(
                Vector3.Distance(start, end) / config.SeparationM) + 1);
        }

        static List<Vector3> GetGroundedPolePoints(Viewer viewer,
            int tileX, int tileZ, Vector3 start, Vector3 end,
            int population)
        {
            var points = new List<Vector3>(population);
            for (int i = 0; i < population; i++)
            {
                float amount = (float)i / (population - 1);
                Vector3 point = Vector3.Lerp(start, end, amount);
                point.Y = viewer.Tiles.LoadAndGetElevation(tileX, tileZ,
                    point.X, point.Z, false);
                points.Add(point);
            }
            return points;
        }

        static void AddPoleShapes(Viewer viewer,
            List<StaticShape> sceneryObjects, List<Vector3> points,
            string shapeFilePath, int tileX, int tileZ,
            ShapeFlags shapeFlags)
        {
            if (String.IsNullOrEmpty(shapeFilePath))
                return;

            Vector3 direction = new Vector3(
                points[points.Count - 1].X - points[0].X, 0,
                -(points[points.Count - 1].Z - points[0].Z));
            if (direction.LengthSquared() > 0.000001f)
                direction.Normalize();
            else
                direction = Vector3.Forward;

            foreach (Vector3 point in points)
            {
                Vector3 xnaPoint = new Vector3(point.X, point.Y,
                    -point.Z);
                Matrix pathMatrix = Matrix.CreateWorld(xnaPoint,
                    direction, Vector3.Up);
                // Telepole pole shapes use local +X as their run direction,
                // while ordinary ORTS path frames use local -Z.
                Matrix matrix = Matrix.CreateRotationY(
                    MathHelper.PiOver2) * pathMatrix;
                var position = new WorldPosition
                {
                    TileX = tileX,
                    TileZ = tileZ,
                    XNAMatrix = matrix,
                };
                // Keep generated poles relative to their owning world tile,
                // like ordinary world objects and Ruler node shapes. Model
                // instancing is batched per world file and assumes one tile.
                sceneryObjects.Add(new StaticShape(viewer,
                    shapeFilePath, position, shapeFlags));
            }
        }

        static List<Vector3> GetWirePath(List<Vector3> polePoints)
        {
            var path = new List<Vector3>();
            for (int span = 0; span + 1 < polePoints.Count; span++)
            {
                Vector3 start = polePoints[span];
                Vector3 end = polePoints[span + 1];
                float length = Vector3.Distance(start, end);
                int subdivisions = Math.Max(4,
                    (int)Math.Ceiling(length));
                float sag = length * SagPerMetre;
                for (int sample = span == 0 ? 0 : 1;
                    sample <= subdivisions; sample++)
                {
                    float amount = (float)sample / subdivisions;
                    Vector3 point = Vector3.Lerp(start, end, amount);
                    point.Y -= 4 * sag * amount * (1 - amount);
                    path.Add(point);
                }
            }
            return path;
        }

        static WorldPosition PointWorldPosition(int tileX, int tileZ,
            Vector3 mstsPoint)
        {
            var position = new WorldPosition
            {
                TileX = tileX,
                TileZ = tileZ,
            };
            position.Location = mstsPoint;
            return position;
        }

        static TrProfile CreateWireProfile(Viewer viewer,
            TelepoleConfig config)
        {
            var profile = new TrProfile(viewer, 0)
            {
                Name = "MSTS Telepole wires",
                Id = "__telepole_wire",
                ObjectType = TrProfile.ProfileObjectType.Static,
                ElevationType = TrProfile.SuperElevationMethod.None,
                LODMethod = TrProfile.LODMethods.CompleteReplacement,
            };
            var lod = new LOD(4000);
            var item = new LODItem("Telepole wires")
            {
                PathFrameMode = LODItem.PathFrameModes.Upright,
                LODMaterial = viewer.MaterialManager.Load("TelepoleWire"),
            };
            for (int i = 0; i < config.Wires.Count; i++)
            {
                Vector3 wire = config.Wires[i];
                // Wire.X is a longitudinal attachment offset. Stock data
                // uses zero; the procedural cross-section is lateral Z/Y.
                float x = wire.Z;
                float y = wire.Y;
                var polyline = new Polyline(profile,
                    "wire" + i, 5);
                AddWireVertex(polyline, x - WireRadiusM,
                    y - WireRadiusM, -1, -1);
                AddWireVertex(polyline, x - WireRadiusM,
                    y + WireRadiusM, -1, 1);
                AddWireVertex(polyline, x + WireRadiusM,
                    y + WireRadiusM, 1, 1);
                AddWireVertex(polyline, x + WireRadiusM,
                    y - WireRadiusM, 1, -1);
                AddWireVertex(polyline, x - WireRadiusM,
                    y - WireRadiusM, -1, -1);
                item.Polylines.Add(polyline);
                item.Accum(polyline.Vertices.Count);
            }
            lod.LODItems.Add(item);
            profile.LODs.Add(lod);
            return profile;
        }

        static void AddWireVertex(Polyline polyline, float x, float y,
            float normalX, float normalY)
        {
            var normal = new Vector3(normalX, normalY, 0);
            normal.Normalize();
            polyline.Vertices.Add(new Vertex(x, y, 0,
                normal.X, normal.Y, normal.Z, 0, 0,
                Vertex.VertexPositionControl.None));
        }

        static float GetObjectRadius(List<Vector3> localPath,
            Vector3 xnaStart, Vector3 xnaEnd, TelepoleConfig config)
        {
            Vector3 center = 0.5f * (xnaStart + xnaEnd);
            float radius = 0;
            foreach (Vector3 point in localPath)
            {
                Vector3 worldPoint = xnaStart + point;
                radius = Math.Max(radius,
                    Vector3.Distance(center, worldPoint));
            }
            float profileRadius = 0;
            foreach (Vector3 wire in config.Wires)
                profileRadius = Math.Max(profileRadius,
                    (float)Math.Sqrt(wire.Y * wire.Y +
                        wire.Z * wire.Z));
            return radius + profileRadius + WireRadiusM;
        }
    }

    /// <summary>
    /// A DynamicTrackPrimitive-compatible container for a sampled multiline.
    /// It preserves the established TrProfile material and LOD lifecycle but
    /// combines each wire into bounded vertex-buffer chunks.
    /// </summary>
    sealed class TelepoleWirePrimitive : DynamicTrackPrimitive
    {
        const int MaximumVerticesPerPrimitive = 30000;

        public TelepoleWirePrimitive(Viewer viewer,
            List<Vector3> path, TrProfile profile, Vector3 xnaEnd,
            float objectRadius)
        {
            TrProfile = profile;
            XNAEnd = xnaEnd;
            ObjectRadius = objectRadius;

            var primitives = new List<ShapePrimitive>();
            LOD lod = (LOD)profile.LODs[0];
            foreach (LODItem item in lod.LODItems)
            {
                foreach (Polyline polyline in item.Polylines)
                    BuildPolylinePrimitives(viewer, primitives, path,
                        polyline, item.LODMaterial);
            }

            ShapePrimitives = primitives.ToArray();
            ShapePrimitiveTransforms = new Matrix[primitives.Count][];
            LODPrimitiveIndexStarts = new[] { 0 };
            LODPrimitiveIndexStops = new[] { primitives.Count };
        }

        public override void PreparePrimitives(Viewer viewer)
        {
            // The sampled multiline was prepared and chunked by the
            // constructor. Rebuilding it as an ordinary DynamicTrackPrimitive
            // would replace it with zero-length geometry because a Telepole
            // has no DynamicTrack section parameters.
        }

        static void BuildPolylinePrimitives(Viewer viewer,
            List<ShapePrimitive> primitives, List<Vector3> path,
            Polyline polyline, Material material)
        {
            int verticesPerSection = polyline.Vertices.Count;
            if (verticesPerSection < 2 || path.Count < 2)
                return;
            int pointsPerPrimitive = Math.Max(2,
                MaximumVerticesPerPrimitive / verticesPerSection);
            for (int start = 0; start + 1 < path.Count;)
            {
                int end = Math.Min(path.Count - 1,
                    start + pointsPerPrimitive - 1);
                primitives.Add(BuildPrimitive(viewer, path, polyline,
                    material, start, end));
                start = end;
            }
        }

        static ShapePrimitive BuildPrimitive(Viewer viewer,
            List<Vector3> path, Polyline polyline, Material material,
            int start, int end)
        {
            int stride = polyline.Vertices.Count;
            int pointCount = end - start + 1;
            var vertices = new VertexPositionNormalTexture[
                pointCount * stride];
            var indices = new short[
                (pointCount - 1) * (stride - 1) * 6];
            int vertexIndex = 0;
            int indexIndex = 0;
            for (int pathIndex = start; pathIndex <= end; pathIndex++)
            {
                Matrix frame = GetUprightFrame(path, pathIndex);
                for (int i = 0; i < stride; i++)
                {
                    Vertex source = (Vertex)polyline.Vertices[i];
                    vertices[vertexIndex].Position = Vector3.Transform(
                        source.Position, frame);
                    vertices[vertexIndex].Normal = Vector3.TransformNormal(
                        source.Normal, frame);
                    vertices[vertexIndex].TextureCoordinate =
                        source.TexCoord;
                    if (pathIndex > start && i > 0)
                    {
                        indices[indexIndex++] = (short)vertexIndex;
                        indices[indexIndex++] = (short)(vertexIndex -
                            1 - stride);
                        indices[indexIndex++] = (short)(vertexIndex - 1);
                        indices[indexIndex++] = (short)vertexIndex;
                        indices[indexIndex++] = (short)(vertexIndex -
                            stride);
                        indices[indexIndex++] = (short)(vertexIndex -
                            1 - stride);
                    }
                    vertexIndex++;
                }
            }

            var indexBuffer = new IndexBuffer(viewer.GraphicsDevice,
                typeof(short), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
            return new ShapePrimitive(material,
                new SharedShape.VertexBufferSet(vertices,
                    viewer.GraphicsDevice), indexBuffer,
                indices.Length / 3, new[] { -1 }, 0);
        }

        static Matrix GetUprightFrame(List<Vector3> path, int index)
        {
            Vector3 direction;
            if (index == 0)
                direction = path[1] - path[0];
            else if (index + 1 == path.Count)
                direction = path[index] - path[index - 1];
            else
                direction = path[index + 1] - path[index - 1];
            direction.Y = 0;
            if (direction.LengthSquared() > 0.000001f)
                direction.Normalize();
            else
                direction = Vector3.Forward;
            return Matrix.CreateWorld(path[index], direction,
                Vector3.Up);
        }
    }
}
