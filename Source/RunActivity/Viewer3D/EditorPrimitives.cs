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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Orts.Viewer3D
{
    public class EditorShapes : StaticShape, IDisposable
    {
        public readonly MouseCrosshair MouseCrosshair;
        public bool MouseCrosshairEnabled { get; set; }
        public bool CrosshairPositionUpdateEnabled { get; set; } = true;

        public readonly HandleX HandleX;
        public readonly HandleY HandleY;
        public readonly HandleZ HandleZ;
        public bool HandleEnabled { get; set; } = true;
        public WorldPosition HandleLocation;

        StaticShape _selectedObject;
        public StaticShape SelectedObject { get; set; }

        public ConcurrentBag<StaticShape> BoundingBoxShapes = new ConcurrentBag<StaticShape>();
        readonly ConcurrentDictionary<(int tileX, int tileZ, int uid, Matrix matrix, int number), BoundingBoxPrimitive> BoundingBoxPrimitives = new ConcurrentDictionary<(int, int, int, Matrix, int), BoundingBoxPrimitive>();
        readonly ConcurrentBag<EditorPrimitive> UnusedPrimitives = new ConcurrentBag<EditorPrimitive>();
        Vector3 CrosshairPosition;

        public EditorShapes(Viewer viewer) : base(viewer, "", null, ShapeFlags.None, null, -1)
        {
            MouseCrosshair = new MouseCrosshair(Viewer, Color.GreenYellow);
            HandleX = new HandleX(Viewer, Color.Red);
            HandleY = new HandleY(Viewer, Color.Blue);
            HandleZ = new HandleZ(Viewer, Color.Green);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (_selectedObject != SelectedObject)
            {
                if (_selectedObject != null)
                    for (var i = 0; i < 5; i++)
                        BoundingBoxPrimitives.TryRemove((_selectedObject.Location.TileX, _selectedObject.Location.TileZ, _selectedObject.Uid, _selectedObject.Location.XNAMatrix, i), out _);

                _selectedObject = SelectedObject;

                if (_selectedObject?.BoundingBox?.Length > 0)
                {
                    for (var i = 0; i < _selectedObject.BoundingBox.Length; i++)
                    {
                        BoundingBoxPrimitives.TryAdd((_selectedObject.Location.TileX, _selectedObject.Location.TileZ, _selectedObject.Uid, _selectedObject.Location.XNAMatrix, i),
                            new BoundingBoxPrimitive(Viewer, _selectedObject.BoundingBox[i], Color.CornflowerBlue));
                    }
                }
            }
            for (var i = 0; i < BoundingBoxPrimitives.Count; i++)
            {
                var bb = BoundingBoxPrimitives.Keys.ElementAtOrDefault(i);
                if (bb != default((int, int, int, Matrix, int))
                    && !BoundingBoxShapes.Any(s => s?.Location.TileX == bb.tileX && s?.Location.TileZ == bb.tileZ && s?.Uid == bb.uid))
                {
                    if (bb.tileX != _selectedObject?.Location.TileX && bb.tileZ != _selectedObject?.Location.TileZ && bb.uid != _selectedObject?.Uid)
                    {
                        if (BoundingBoxPrimitives.TryRemove(bb, out var primitive))
                            UnusedPrimitives.Add(primitive);
                    }
                }
            }
    
            for (var j = 0; j < BoundingBoxShapes.Count; j++)
            {
                var s = BoundingBoxShapes.ElementAtOrDefault(j);
                if (s?.BoundingBox == null || s.BoundingBox.Length == 0)
                    continue;

                if (!BoundingBoxPrimitives.Keys.Any(bb => s.Location.TileX == bb.tileX && s.Location.TileZ == bb.tileZ && s.Uid == bb.uid))
                {
                    for (var i = 0; i < s.BoundingBox.Length; i++)
                    {
                        BoundingBoxPrimitives.TryAdd((s.Location.TileX, s.Location.TileZ, s.Uid, s.Location.XNAMatrix, i),
                            new BoundingBoxPrimitive(Viewer, s.BoundingBox[i], Color.MediumVioletRed));
                    }
                }
            }

            if (BoundingBoxPrimitives?.Count > 0)
            {
                foreach (var boundingBox in BoundingBoxPrimitives.Keys)
                {
                    var dTileX = boundingBox.tileX - Viewer.Camera.TileX;
                    var dTileZ = boundingBox.tileZ - Viewer.Camera.TileZ;
                    var xnaDTileTranslation = boundingBox.matrix;
                    xnaDTileTranslation.M41 += dTileX * 2048;
                    xnaDTileTranslation.M43 -= dTileZ * 2048;
                    xnaDTileTranslation = BoundingBoxPrimitives[boundingBox].ComplexTransform * xnaDTileTranslation;
                    frame.AddPrimitive(BoundingBoxPrimitives[boundingBox].Material, BoundingBoxPrimitives[boundingBox], RenderPrimitiveGroup.Labels, ref xnaDTileTranslation);
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
            if (HandleEnabled)
            {
                var handleLocation = HandleLocation ?? _selectedObject?.Location;
                if (handleLocation != null)
                {
                    var dTileX = handleLocation.TileX - Viewer.Camera.TileX;
                    var dTileZ = handleLocation.TileZ - Viewer.Camera.TileZ;
                    var xnaDTileTranslation = handleLocation.XNAMatrix;
                    xnaDTileTranslation.M41 += dTileX * 2048;
                    xnaDTileTranslation.M43 -= dTileZ * 2048;
                    frame.AddPrimitive(HandleX.Material, HandleX, RenderPrimitiveGroup.Overlay, ref xnaDTileTranslation);
                    frame.AddPrimitive(HandleY.Material, HandleY, RenderPrimitiveGroup.Overlay, ref xnaDTileTranslation);
                    frame.AddPrimitive(HandleZ.Material, HandleZ, RenderPrimitiveGroup.Overlay, ref xnaDTileTranslation);
                }
            }
        }

        internal override void Mark()
        {
            MouseCrosshair.Mark();
            if (BoundingBoxPrimitives?.Count > 0)
            {
                foreach (var selectedObject in BoundingBoxPrimitives)
                {
                    selectedObject.Value.Mark();
                }
            }
        }

        public void Dispose()
        {
            MouseCrosshair?.Dispose();
            if (BoundingBoxPrimitives?.Count > 0)
            {
                foreach (var selectedObject in BoundingBoxPrimitives)
                {
                    selectedObject.Value.Dispose();
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
            Material = viewer.MaterialManager.Load("EditorPrimitive");
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
                new VertexPositionColor(new Vector3(0, 0, -5), Color.Red),
                new VertexPositionColor(new Vector3(0, 0, 5), Color.Cyan),
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

    [CallOnThread("Loader")]
    public class HandleX : EditorPrimitive
    {
        public HandleX(Viewer viewer, Color color)
        {
            var vertexData = GetVertexData(color);
            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), vertexData.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertexData);
            PrimitiveCount = VertexBuffer.VertexCount / 2;
            PrimitiveType = PrimitiveType.TriangleList;
            Material = viewer.MaterialManager.Load("EditorPrimitive");
        }

        protected virtual VertexPositionColor[] GetVertexData(Color color) => GetVertexData(0, 1, 2, color);
        protected VertexPositionColor[] GetVertexData(int x, int y, int z, Color color)
        {
            var l = 5f;
            var d = 0.1f;
            var a = l / 5;
            var b = a / 4;
            var c = l - a;
            var data = new float[][]
            {
                new[] { 0, +d, 0 },
                new[] { c, +d, 0 },
                new[] { 0, -d, 0 },
                new[] { 0, -d, 0 },
                new[] { c, +d, 0 },
                new[] { c, -d, 0 },

                new[] { 0, 0, +d },
                new[] { c, 0, +d },
                new[] { 0, 0, -d },
                new[] { 0, 0, -d },
                new[] { c, 0, +d },
                new[] { c, 0, -d },

                new[] { l,      0, 0 },
                new[] { l - a, +b, +b },
                new[] { l - a, -b, +b },
                new[] { l,      0, 0 },
                new[] { l - a, -b, +b },
                new[] { l - a, -b, -b },
                new[] { l,      0, 0 },
                new[] { l - a, -b, -b },
                new[] { l - a, +b, -b },
                new[] { l,      0, 0 },
                new[] { l - a, +b, -b },
                new[] { l - a, +b, +b },

                new[] { l - a, +b, +b },
                new[] { l - a, +b, -b },
                new[] { l - a, -b, -b },
                new[] { l - a, -b, -b },
                new[] { l - a, +b, -b },
                new[] { l - a, -b, +b },
            };
            var vertexData = new VertexPositionColor[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                vertexData[i] = new VertexPositionColor(new Vector3(data[i][x], data[i][y], data[i][z]), color);
            }
            return vertexData;
        }
    }

    [CallOnThread("Loader")]
    public class HandleY : HandleX
    {
        public HandleY(Viewer viewer, Color color) : base(viewer, color) { }
        protected override VertexPositionColor[] GetVertexData(Color color) => GetVertexData(2, 0, 1, color);
    }

    [CallOnThread("Loader")]
    public class HandleZ : HandleX
    {
        public HandleZ(Viewer viewer, Color color) : base(viewer, color) { }
        protected override VertexPositionColor[] GetVertexData(Color color) => GetVertexData(1, 2, 0, color);
    }
}
