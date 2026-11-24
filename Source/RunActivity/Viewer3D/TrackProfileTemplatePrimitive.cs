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
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Common;

namespace Orts.Viewer3D
{
    sealed class TrackProfileBuiltPrimitive
    {
        public ShapePrimitive Primitive;
        public Matrix[] Transforms;
    }

    sealed class TrackProfileObjVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
    }

    sealed class TrackProfileObjMesh
    {
        public string FilePath;
        public readonly List<TrackProfileObjVertex> Vertices =
            new List<TrackProfileObjVertex>();
        public float MinimumZM;
        public float MaximumZM;
        public float MinimumV;
        public float MaximumV;

        public float DepthM { get { return MaximumZM - MinimumZM; } }
    }

    static class TrackProfileObjReader
    {
        static readonly object CacheLock = new object();
        static readonly Dictionary<string, TrackProfileObjMesh> Cache =
            new Dictionary<string, TrackProfileObjMesh>(
                StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> FailedFiles =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static TrackProfileObjMesh Get(TrProfile profile,
            string shapeName)
        {
            string sourceDirectory = string.IsNullOrEmpty(
                profile.SourceFilePath)
                ? "" : Path.GetDirectoryName(profile.SourceFilePath);
            string relativeName = shapeName.Replace('/',
                Path.DirectorySeparatorChar);
            string filePath = Path.GetFullPath(Path.Combine(
                sourceDirectory ?? "", relativeName));

            lock (CacheLock)
            {
                TrackProfileObjMesh cached;
                if (Cache.TryGetValue(filePath, out cached))
                    return cached;
                if (FailedFiles.Contains(filePath))
                    return null;

                try
                {
                    cached = Load(filePath);
                    Cache.Add(filePath, cached);
                    return cached;
                }
                catch (Exception error)
                {
                    FailedFiles.Add(filePath);
                    Trace.WriteLine(new FileLoadException(filePath, error));
                    return null;
                }
            }
        }

        static TrackProfileObjMesh Load(string filePath)
        {
            var positions = new List<Vector3>();
            var textureCoordinates = new List<Vector2>();
            var normals = new List<Vector3>();
            var mesh = new TrackProfileObjMesh
            {
                FilePath = filePath,
                MinimumZM = float.MaxValue,
                MaximumZM = float.MinValue,
                MinimumV = float.MaxValue,
                MaximumV = float.MinValue,
            };

            int lineNumber = 0;
            foreach (string sourceLine in File.ReadLines(filePath))
            {
                lineNumber++;
                string line = sourceLine;
                int comment = line.IndexOf('#');
                if (comment >= 0)
                    line = line.Substring(0, comment);
                string[] values = line.Split(new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 0)
                    continue;

                switch (values[0])
                {
                    case "v":
                        if (values.Length < 4)
                            throw InvalidRecord(filePath, lineNumber, "v");
                        positions.Add(new Vector3(
                            ReadFinite(values[1], filePath, lineNumber),
                            ReadFinite(values[2], filePath, lineNumber),
                            ReadFinite(values[3], filePath, lineNumber)));
                        break;
                    case "vt":
                        if (values.Length < 3)
                            throw InvalidRecord(filePath, lineNumber, "vt");
                        textureCoordinates.Add(new Vector2(
                            ReadFinite(values[1], filePath, lineNumber),
                            ReadFinite(values[2], filePath, lineNumber)));
                        break;
                    case "vn":
                        if (values.Length < 4)
                            throw InvalidRecord(filePath, lineNumber, "vn");
                        normals.Add(new Vector3(
                            ReadFinite(values[1], filePath, lineNumber),
                            ReadFinite(values[2], filePath, lineNumber),
                            ReadFinite(values[3], filePath, lineNumber)));
                        break;
                    case "f":
                        if (values.Length != 4)
                            throw InvalidRecord(filePath, lineNumber, "f");
                        for (int i = 1; i < 4; i++)
                            mesh.Vertices.Add(ReadFaceVertex(values[i],
                                positions, textureCoordinates, normals,
                                filePath, lineNumber));
                        break;
                }
            }

            if (mesh.Vertices.Count == 0)
                throw new InvalidDataException(
                    "OBJ file contains no complete triangles.");

            foreach (TrackProfileObjVertex vertex in mesh.Vertices)
            {
                mesh.MinimumZM = Math.Min(mesh.MinimumZM, vertex.Position.Z);
                mesh.MaximumZM = Math.Max(mesh.MaximumZM, vertex.Position.Z);
                mesh.MinimumV = Math.Min(mesh.MinimumV,
                    vertex.TextureCoordinate.Y);
                mesh.MaximumV = Math.Max(mesh.MaximumV,
                    vertex.TextureCoordinate.Y);
            }
            return mesh;
        }

        static TrackProfileObjVertex ReadFaceVertex(string value,
            List<Vector3> positions, List<Vector2> textureCoordinates,
            List<Vector3> normals, string filePath, int lineNumber)
        {
            string[] indices = value.Split('/');
            if (indices.Length != 3 || indices[0].Length == 0 ||
                indices[1].Length == 0 || indices[2].Length == 0)
                throw InvalidRecord(filePath, lineNumber, "f");

            int positionIndex = ReadIndex(indices[0], positions.Count,
                filePath, lineNumber);
            int textureIndex = ReadIndex(indices[1], textureCoordinates.Count,
                filePath, lineNumber);
            int normalIndex = ReadIndex(indices[2], normals.Count,
                filePath, lineNumber);
            return new TrackProfileObjVertex
            {
                Position = positions[positionIndex],
                TextureCoordinate = textureCoordinates[textureIndex],
                Normal = normals[normalIndex],
            };
        }

        static int ReadIndex(string value, int count, string filePath,
            int lineNumber)
        {
            int result;
            if (!int.TryParse(value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out result) ||
                result <= 0 || result > count)
                throw InvalidRecord(filePath, lineNumber, "f");
            return result - 1;
        }

        static float ReadFinite(string value, string filePath, int lineNumber)
        {
            float result;
            if (!float.TryParse(value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out result) ||
                float.IsNaN(result) || float.IsInfinity(result))
                throw InvalidRecord(filePath, lineNumber, "number");
            return result;
        }

        static InvalidDataException InvalidRecord(string filePath,
            int lineNumber, string record)
        {
            return new InvalidDataException(string.Format(
                CultureInfo.InvariantCulture,
                "Invalid {0} record in {1} at line {2}.",
                record, filePath, lineNumber));
        }
    }

    static class TrackProfileTemplatePrimitiveBuilder
    {
        const int MaximumVertexCount = ushort.MaxValue;
        const float MinimumTemplateDepthM = 0.000001f;
        const float DistanceToleranceM = 0.0001f;

        sealed class SharedGeometry
        {
            public TrackProfileObjMesh Mesh;
            public readonly List<Matrix> Transforms = new List<Matrix>();
        }

        public static List<TrackProfileBuiltPrimitive> Build(Viewer viewer,
            DynamicTrackPrimitive source, LODItem lodItem)
        {
            source.InitializeSectionGeometry();
            var result = new List<TrackProfileBuiltPrimitive>();
            var vertices = new List<VertexPositionNormalTexture>();
            var indices = new List<short>();
            var sharedGeometry = new Dictionary<string, SharedGeometry>(
                StringComparer.OrdinalIgnoreCase);

            foreach (TrackProfileTemplate3D template in lodItem.Templates3D)
            {
                if (template.GeometryMode ==
                    TrackProfileTemplate3D.GeometryModes.Shared)
                    AppendSharedTemplate(source, lodItem, template,
                        sharedGeometry);
                else
                    AppendBakedTemplate(source, lodItem, template,
                        vertices, indices);
            }

            if (indices.Count > 0)
                result.Add(CreatePrimitive(viewer, lodItem, vertices,
                    indices, null));

            foreach (SharedGeometry shared in sharedGeometry.Values)
            {
                var sharedVertices = new List<VertexPositionNormalTexture>();
                var sharedIndices = new List<short>();
                AppendSharedSource(shared.Mesh, sharedVertices, sharedIndices);
                if (sharedIndices.Count > 0)
                    result.Add(CreatePrimitive(viewer, lodItem, sharedVertices,
                        sharedIndices, shared.Transforms.ToArray()));
            }
            return result;
        }

        static TrackProfileBuiltPrimitive CreatePrimitive(Viewer viewer,
            LODItem lodItem, List<VertexPositionNormalTexture> vertices,
            List<short> indices, Matrix[] transforms)
        {
            var indexBuffer = new IndexBuffer(viewer.GraphicsDevice,
                typeof(short), indices.Count, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices.ToArray());
            var primitive = new ShapePrimitive(lodItem.LODMaterial,
                new SharedShape.VertexBufferSet(vertices.ToArray(),
                    viewer.GraphicsDevice), indexBuffer,
                indices.Count / 3, new[] { -1 }, 0);
            return new TrackProfileBuiltPrimitive
            {
                Primitive = primitive,
                Transforms = transforms,
            };
        }

        static void AppendBakedTemplate(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            List<VertexPositionNormalTexture> vertices, List<short> indices)
        {
            List<TrackProfileObjMesh> meshes = LoadMeshes(source.TrProfile,
                template);
            if (meshes.Count == 0)
                return;

            switch (template.GenerationMode)
            {
                case TrackProfileTemplate3D.GenerationModes.Sweep:
                    AppendSweep(source, lodItem, template, meshes,
                        vertices, indices);
                    break;
                case TrackProfileTemplate3D.GenerationModes.Stretch:
                    AppendStretch(source, lodItem, template, meshes,
                        vertices, indices);
                    break;
                case TrackProfileTemplate3D.GenerationModes.Repeat:
                    AppendRepeat(source, lodItem, template, meshes,
                        vertices, indices, null);
                    break;
                case TrackProfileTemplate3D.GenerationModes.Place:
                    AppendPlacements(source, lodItem, template, meshes,
                        vertices, indices, null);
                    break;
            }
        }

        static void AppendSharedTemplate(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            Dictionary<string, SharedGeometry> output)
        {
            List<TrackProfileObjMesh> meshes = LoadMeshes(source.TrProfile,
                template);
            if (meshes.Count == 0)
                return;

            if (template.GenerationMode ==
                TrackProfileTemplate3D.GenerationModes.Repeat)
                AppendRepeat(source, lodItem, template, meshes,
                    null, null, output);
            else
                AppendPlacements(source, lodItem, template, meshes,
                    null, null, output);
        }

        static List<TrackProfileObjMesh> LoadMeshes(TrProfile profile,
            TrackProfileTemplate3D template)
        {
            var result = new List<TrackProfileObjMesh>();
            foreach (string shape in template.Shapes)
                result.Add(TrackProfileObjReader.Get(profile, shape));
            return result;
        }

        static void AppendSweep(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            List<TrackProfileObjMesh> meshes,
            List<VertexPositionNormalTexture> vertices, List<short> indices)
        {
            float pathLengthM = source.GetPathLengthM();
            float startM = 0;
            int copyIndex = 0;
            while (startM < pathLengthM - MinimumTemplateDepthM)
            {
                TrackProfileObjMesh mesh = SelectMesh(source, template,
                    meshes, copyIndex, SelectionObjectIndex(source, false));
                if (mesh == null || mesh.DepthM <= MinimumTemplateDepthM)
                    return;
                float endM = Math.Min(pathLengthM, startM + mesh.DepthM);
                if (!AppendDeformed(source, lodItem, template, mesh,
                        startM, endM, true, false, vertices, indices))
                    return;
                startM = endM;
                copyIndex++;
            }
        }

        static void AppendStretch(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            List<TrackProfileObjMesh> meshes,
            List<VertexPositionNormalTexture> vertices, List<short> indices)
        {
            TrackProfileObjMesh mesh = SelectMesh(source, template, meshes,
                0, SelectionObjectIndex(source, true));
            if (mesh != null)
                AppendDeformed(source, lodItem, template, mesh, 0,
                    source.GetPathLengthM(), false,
                    source.PathContext.IsPointPath, vertices, indices);
        }

        static bool AppendDeformed(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            TrackProfileObjMesh mesh, float startM, float endM,
            bool usePathLengthTexture, bool useAveragedFrames,
            List<VertexPositionNormalTexture> vertices, List<short> indices)
        {
            if (mesh.DepthM <= MinimumTemplateDepthM ||
                vertices.Count + mesh.Vertices.Count > MaximumVertexCount)
            {
                Trace.TraceWarning(
                    "Ignored oversized or depthless generated Template3D mesh {0}.",
                    mesh.FilePath);
                return false;
            }

            int firstVertex = vertices.Count;
            foreach (TrackProfileObjVertex vertex in mesh.Vertices)
            {
                float fraction = (mesh.MaximumZM - vertex.Position.Z) /
                    mesh.DepthM;
                float pathDistanceM = startM + fraction * (endM - startM);
                Matrix frame = source.GetProfileFrame(
                    pathDistanceM - template.OffsetM.Z,
                    lodItem.PathFrameMode, useAveragedFrames);
                Vector3 localPosition = new Vector3(
                    vertex.Position.X + template.OffsetM.X,
                    vertex.Position.Y + template.OffsetM.Y, 0);
                Vector2 textureCoordinate = vertex.TextureCoordinate;
                if (usePathLengthTexture)
                    textureCoordinate.Y = mesh.MinimumV +
                        (mesh.MaximumV - mesh.MinimumV) * pathDistanceM;
                vertices.Add(new VertexPositionNormalTexture(
                    Vector3.Transform(localPosition, frame),
                    TransformNormal(vertex.Normal, frame),
                    textureCoordinate));
            }
            AddSequentialTriangles(indices, firstVertex, mesh.Vertices.Count);
            return true;
        }

        static void AppendRepeat(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            List<TrackProfileObjMesh> meshes,
            List<VertexPositionNormalTexture> vertices, List<short> indices,
            Dictionary<string, SharedGeometry> sharedOutput)
        {
            int copyIndex = 0;
            float distanceM = template.PhaseM;
            while (distanceM < 0)
            {
                distanceM += template.SpacingM;
                copyIndex++;
            }
            float pathLengthM = source.GetPathLengthM();
            for (; distanceM <= pathLengthM + DistanceToleranceM;
                distanceM += template.SpacingM, copyIndex++)
            {
                TrackProfileObjMesh mesh = SelectMesh(source, template,
                    meshes, copyIndex, SelectionObjectIndex(source, false));
                if (mesh == null)
                    continue;
                Matrix frame = source.GetProfileFrame(distanceM,
                    lodItem.PathFrameMode, false);
                AppendRigid(mesh, template.OffsetM, frame, false,
                    vertices, indices, sharedOutput);
            }
        }

        static void AppendPlacements(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            List<TrackProfileObjMesh> meshes,
            List<VertexPositionNormalTexture> vertices, List<short> indices,
            Dictionary<string, SharedGeometry> sharedOutput)
        {
            int copyIndex = 0;
            foreach (TrackProfileTemplatePlacement placement in
                template.Placements)
            {
                if (placement.Location ==
                    TrackProfileTemplate3D.PlacementLocation.Nodes)
                {
                    if (source.PathContext.OwnStartNode)
                        AppendPlacement(source, lodItem, template, meshes,
                            0, true, false, placement.Facing, copyIndex++,
                            vertices, indices, sharedOutput);
                    if (source.PathContext.OwnEndNode)
                        AppendPlacement(source, lodItem, template, meshes,
                            source.GetPathLengthM(), true, true,
                            placement.Facing, copyIndex++, vertices, indices,
                            sharedOutput);
                    continue;
                }

                if (placement.Location ==
                        TrackProfileTemplate3D.PlacementLocation.PathStart &&
                    !source.PathContext.OwnPathStart)
                    continue;
                if (placement.Location ==
                        TrackProfileTemplate3D.PlacementLocation.PathEnd &&
                    !source.PathContext.OwnPathEnd)
                    continue;

                bool atEnd = placement.Location ==
                        TrackProfileTemplate3D.PlacementLocation.SpanEnd ||
                    placement.Location ==
                        TrackProfileTemplate3D.PlacementLocation.PathEnd;
                AppendPlacement(source, lodItem, template, meshes,
                    atEnd ? source.GetPathLengthM() : 0, false, atEnd,
                    placement.Facing, copyIndex++, vertices, indices,
                    sharedOutput);
            }
        }

        static void AppendPlacement(DynamicTrackPrimitive source,
            LODItem lodItem, TrackProfileTemplate3D template,
            List<TrackProfileObjMesh> meshes, float distanceM,
            bool averagedFrame, bool atEnd,
            TrackProfileTemplate3D.PlacementFacing facing, int copyIndex,
            List<VertexPositionNormalTexture> vertices, List<short> indices,
            Dictionary<string, SharedGeometry> sharedOutput)
        {
            int selectionObjectIndex = SelectionObjectIndex(source,
                averagedFrame && atEnd);
            TrackProfileObjMesh mesh = SelectMesh(source, template, meshes,
                copyIndex, selectionObjectIndex);
            if (mesh == null)
                return;

            bool reverse = facing ==
                TrackProfileTemplate3D.PlacementFacing.AgainstPath ||
                (facing == TrackProfileTemplate3D.PlacementFacing.Outward &&
                    !atEnd) ||
                (facing == TrackProfileTemplate3D.PlacementFacing.Inward &&
                    atEnd);
            Matrix frame = source.GetProfileFrame(distanceM,
                lodItem.PathFrameMode, averagedFrame);
            AppendRigid(mesh, template.OffsetM, frame, reverse,
                vertices, indices, sharedOutput);
        }

        static void AppendRigid(TrackProfileObjMesh mesh, Vector3 offsetM,
            Matrix frame, bool reverse,
            List<VertexPositionNormalTexture> vertices, List<short> indices,
            Dictionary<string, SharedGeometry> sharedOutput)
        {
            Matrix placement = reverse
                ? Matrix.CreateRotationY(MathHelper.Pi) * frame : frame;
            if (sharedOutput != null)
            {
                SharedGeometry shared;
                if (!sharedOutput.TryGetValue(mesh.FilePath, out shared))
                {
                    shared = new SharedGeometry { Mesh = mesh };
                    sharedOutput.Add(mesh.FilePath, shared);
                }
                Matrix offset = Matrix.CreateTranslation(offsetM);
                shared.Transforms.Add(offset * placement);
                return;
            }

            if (vertices.Count + mesh.Vertices.Count > MaximumVertexCount)
            {
                Trace.TraceWarning(
                    "Ignored oversized generated Template3D mesh {0}.",
                    mesh.FilePath);
                return;
            }
            int firstVertex = vertices.Count;
            foreach (TrackProfileObjVertex vertex in mesh.Vertices)
            {
                Vector3 position = vertex.Position + offsetM;
                vertices.Add(new VertexPositionNormalTexture(
                    Vector3.Transform(position, placement),
                    TransformNormal(vertex.Normal, placement),
                    vertex.TextureCoordinate));
            }
            AddSequentialTriangles(indices, firstVertex, mesh.Vertices.Count);
        }

        static void AppendSharedSource(TrackProfileObjMesh mesh,
            List<VertexPositionNormalTexture> vertices, List<short> indices)
        {
            if (mesh.Vertices.Count > MaximumVertexCount)
            {
                Trace.TraceWarning(
                    "Ignored oversized shared Template3D mesh {0}.",
                    mesh.FilePath);
                return;
            }
            foreach (TrackProfileObjVertex vertex in mesh.Vertices)
                vertices.Add(new VertexPositionNormalTexture(vertex.Position,
                    vertex.Normal, vertex.TextureCoordinate));
            AddSequentialTriangles(indices, 0, mesh.Vertices.Count);
        }

        static TrackProfileObjMesh SelectMesh(DynamicTrackPrimitive source,
            TrackProfileTemplate3D template,
            List<TrackProfileObjMesh> meshes, int copyIndex,
            int objectIndex)
        {
            if (meshes.Count == 0)
                return null;
            int selectedIndex;
            switch (template.ShapeSelectionMode)
            {
                case TrackProfileTemplate3D.ShapeSelectionModes.ByObject:
                    selectedIndex = PositiveModulo(objectIndex, meshes.Count);
                    break;
                case TrackProfileTemplate3D.ShapeSelectionModes.Cycle:
                    selectedIndex = PositiveModulo(copyIndex, meshes.Count);
                    break;
                case TrackProfileTemplate3D.ShapeSelectionModes.DeterministicRandom:
                    selectedIndex = (int)(StableHash(string.Format(
                        CultureInfo.InvariantCulture, "{0}:{1}:{2}",
                        source.TrProfile.Id, objectIndex, copyIndex)) %
                        (uint)meshes.Count);
                    break;
                default:
                    selectedIndex = 0;
                    break;
            }
            return meshes[selectedIndex];
        }

        static int SelectionObjectIndex(DynamicTrackPrimitive source,
            bool nextNode)
        {
            int result = source.PathContext.ObjectIndex;
            if (source.PathContext.IsPointPath)
                result += source.PathContext.SpanIndex + (nextNode ? 1 : 0);
            return result;
        }

        static int PositiveModulo(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        static uint StableHash(string value)
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;
            uint result = offsetBasis;
            foreach (byte valueByte in Encoding.UTF8.GetBytes(value ?? ""))
            {
                result ^= valueByte;
                result *= prime;
            }
            return result;
        }

        static Vector3 TransformNormal(Vector3 normal, Matrix matrix)
        {
            Vector3 result = Vector3.TransformNormal(normal, matrix);
            if (result.LengthSquared() > 0)
                result.Normalize();
            return result;
        }

        static void AddSequentialTriangles(List<short> indices,
            int firstVertex, int vertexCount)
        {
            // OBJ front faces are counter-clockwise. ORTS/XNA scenery uses
            // clockwise front faces, so reverse each imported triangle once
            // at the shared index-generation boundary.
            for (int vertex = 0; vertex < vertexCount; vertex += 3)
            {
                AddIndex(indices, firstVertex + vertex);
                AddIndex(indices, firstVertex + vertex + 2);
                AddIndex(indices, firstVertex + vertex + 1);
            }
        }

        static void AddIndex(List<short> indices, int index)
        {
            indices.Add(unchecked((short)(ushort)index));
        }
    }
}
