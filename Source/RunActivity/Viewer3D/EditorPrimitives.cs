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
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D
{
    public class EditorShapes : StaticShape, IDisposable
    {
        public readonly MouseCrosshair MouseCrosshair;
        public bool MouseCrosshairEnabled { get; set; }
        public StaticShape SelectedObject { get; set; }
        StaticShape _selectedObject;
        BoundingBoxPrimitive SelectedObjectBoundingBoxPrimitive;
        List<EditorPrimitive> UnusedPrimitives = new List<EditorPrimitive>();

        public EditorShapes(Viewer viewer) : base(viewer, "", null, ShapeFlags.None, null, -1)
        {
            MouseCrosshair = new MouseCrosshair(Viewer, Color.GreenYellow);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (_selectedObject != SelectedObject)
            {
                _selectedObject = SelectedObject;
                UnusedPrimitives.Add(SelectedObjectBoundingBoxPrimitive);
                SelectedObjectBoundingBoxPrimitive = _selectedObject?.BoundingBox == null ? null :
                    new BoundingBoxPrimitive(Viewer, _selectedObject.BoundingBox.Value, Color.CornflowerBlue);
            }
            if (SelectedObjectBoundingBoxPrimitive != null)
            {
                var dTileX = _selectedObject.Location.TileX - Viewer.Camera.TileX;
                var dTileZ = _selectedObject.Location.TileZ - Viewer.Camera.TileZ;
                var mstsLocation = _selectedObject.Location.Location + new Vector3(dTileX * 2048, 0, dTileZ * 2048);
                var xnaMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
                frame.AddPrimitive(SelectedObjectBoundingBoxPrimitive.Material, SelectedObjectBoundingBoxPrimitive, RenderPrimitiveGroup.Labels, ref xnaMatrix);
            }
            if (MouseCrosshairEnabled)
            {
                var mouseCrosshairPosition = (Viewer.Camera as ViewerCamera)?.GetCursorTerrainIntersection() ?? Viewer.NearPoint;
                var mouseCrosshairMatrix = Matrix.CreateTranslation(mouseCrosshairPosition);
                frame.AddPrimitive(MouseCrosshair.Material, MouseCrosshair, RenderPrimitiveGroup.World, ref mouseCrosshairMatrix);
            }
        }

        internal override void Mark()
        {
            MouseCrosshair.Mark();
            SelectedObjectBoundingBoxPrimitive.Mark();
        }

        public void Dispose()
        {
            MouseCrosshair?.Dispose();
            SelectedObjectBoundingBoxPrimitive?.Dispose();
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
