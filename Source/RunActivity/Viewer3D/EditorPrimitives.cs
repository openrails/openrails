// COPYRIGHT 2023 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D
{
    public class EditorShapes : StaticShape, IDisposable
    {
        public readonly MouseCrosshair MouseCrosshair;
        public bool MouseCrosshairEnabled { get; set; }
        public bool CrosshairPositionUpdateEnabled { get; set; } = true;
        public StaticShape SelectedObject { get; set; }
        StaticShape _selectedObject;
        readonly List<BoundingBoxPrimitive> SelectedObjectBoundingBoxPrimitives = new List<BoundingBoxPrimitive>();
        readonly List<EditorPrimitive> UnusedPrimitives = new List<EditorPrimitive>();
        Vector3 CrosshairPosition;

        public EditorShapes(Viewer viewer) : base(viewer, "", null, ShapeFlags.None, null, -1)
        {
            MouseCrosshair = new MouseCrosshair(Viewer, Color.GreenYellow);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (_selectedObject != SelectedObject)
            {
                _selectedObject = SelectedObject;
                if (UnusedPrimitives.Count > 0)
                {
                    foreach (var primitive in UnusedPrimitives)
                    {
                        //primitive.Dispose();
                    }
                    //UnusedPrimitives.Clear();
                }
                UnusedPrimitives.AddRange(SelectedObjectBoundingBoxPrimitives);
                SelectedObjectBoundingBoxPrimitives.Clear();
                if (_selectedObject?.BoundingBox?.Length > 0)
                {
                    foreach (var boundingBox in _selectedObject.BoundingBox)
                    {
                        SelectedObjectBoundingBoxPrimitives.Add(new BoundingBoxPrimitive(Viewer, boundingBox, Color.CornflowerBlue));
                    }
                }
            }
            if (SelectedObjectBoundingBoxPrimitives?.Count > 0)
            {
                foreach (var boundingBox in SelectedObjectBoundingBoxPrimitives)
                {
                    var dTileX = _selectedObject.Location.TileX - Viewer.Camera.TileX;
                    var dTileZ = _selectedObject.Location.TileZ - Viewer.Camera.TileZ;
                    var xnaDTileTranslation = _selectedObject.Location.XNAMatrix;
                    xnaDTileTranslation.M41 += dTileX * 2048;
                    xnaDTileTranslation.M43 -= dTileZ * 2048;
                    xnaDTileTranslation = boundingBox.ComplexTransform * xnaDTileTranslation;
                    frame.AddPrimitive(boundingBox.Material, boundingBox, RenderPrimitiveGroup.Labels, ref xnaDTileTranslation);
                }
            }
            if (MouseCrosshairEnabled)
            {
                if (CrosshairPositionUpdateEnabled)
                {
                    CrosshairPosition = Viewer.TerrainPoint;
                }
                var mouseCrosshairMatrix = Matrix.CreateTranslation(CrosshairPosition);
                frame.AddPrimitive(MouseCrosshair.Material, MouseCrosshair, RenderPrimitiveGroup.World, ref mouseCrosshairMatrix);
            }
        }

        internal override void Mark()
        {
            MouseCrosshair.Mark();
            if (SelectedObjectBoundingBoxPrimitives?.Count > 0)
            {
                foreach (var selectedObject in SelectedObjectBoundingBoxPrimitives)
                {
                    selectedObject.Mark();
                }
            }
        }

        public void Dispose()
        {
            MouseCrosshair?.Dispose();
            if (SelectedObjectBoundingBoxPrimitives?.Count > 0)
            {
                foreach (var selectedObject in SelectedObjectBoundingBoxPrimitives)
                {
                    selectedObject.Dispose();
                }
            }
        }
    }

    public class EditorPrimitive : ShapePrimitive
    {
        protected PrimitiveType PrimitiveType;

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            if (IndexBuffer != null)
            {
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType, 0, 0, PrimitiveCount);
            }
            else
            {
                graphicsDevice.DrawPrimitives(PrimitiveType, 0, PrimitiveCount);
            }
        }
    }

    [CallOnThread("Loader")]
    public class BoundingBoxPrimitive : EditorPrimitive
    {
        static IndexBuffer BoundingBoxIndexBuffer;
        public readonly Matrix ComplexTransform;

        public BoundingBoxPrimitive(Viewer viewer, BoundingBox boundingBox, Color color)
        {
            var vertexData = new VertexPositionColor[]
            {
                new VertexPositionColor(boundingBox.Min, color),
                new VertexPositionColor(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.Z), color),
                new VertexPositionColor(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Min.Z), color),
                new VertexPositionColor(new Vector3(boundingBox.Min.X, boundingBox.Max.Y, boundingBox.Max.Z), color),
                new VertexPositionColor(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Min.Z), color),
                new VertexPositionColor(new Vector3(boundingBox.Max.X, boundingBox.Min.Y, boundingBox.Max.Z), color),
                new VertexPositionColor(new Vector3(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Min.Z), color),
                new VertexPositionColor(boundingBox.Max, color)
            };
            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), vertexData.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertexData);
            if (BoundingBoxIndexBuffer == null)
            {
                var indexData = new short[] { 0, 1, 0, 2, 0, 4, 1, 3, 1, 5, 2, 3, 2, 6, 3, 7, 4, 5, 4, 6, 5, 7, 6, 7 };
                BoundingBoxIndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                BoundingBoxIndexBuffer.SetData(indexData);
            }
            IndexBuffer = BoundingBoxIndexBuffer;
            PrimitiveCount = IndexBuffer.IndexCount / 2;
            PrimitiveType = PrimitiveType.LineList;
            Material = viewer.MaterialManager.Load("DebugNormals");
            ComplexTransform = boundingBox.ComplexTransform;
        }
    }

    [CallOnThread("Loader")]
    public class MouseCrosshair : EditorPrimitive
    {
        public MouseCrosshair(Viewer viewer, Color color)
        {
            var vertexData = new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(-5, 0, 0), color),
                new VertexPositionColor(new Vector3(5, 0, 0), color),
                new VertexPositionColor(new Vector3(0, 0, -5), color),
                new VertexPositionColor(new Vector3(0, 0, 5), color),
                new VertexPositionColor(new Vector3(0, 0, 0), color),
                new VertexPositionColor(new Vector3(0, 20, 0), color)
            };
            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), vertexData.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertexData);
            PrimitiveCount = VertexBuffer.VertexCount / 2;
            PrimitiveType = PrimitiveType.LineList;
            Material = viewer.MaterialManager.Load("DebugNormals");
        }
    }
}
