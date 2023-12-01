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
using System.Linq;

namespace Orts.Viewer3D
{
    public class EditorShapes : StaticShape, IDisposable
    {
        readonly MouseCrosshair MouseCrosshair;
        public bool MouseCrosshairEnabled { get; set; }
        public bool CrosshairPositionUpdateEnabled { get; set; } = true;

        readonly HandleX HandleX;
        readonly HandleY HandleY;
        readonly HandleZ HandleZ;
        public bool HandleEnabled { get; set; } = true;
        public WorldPosition HandleLocation { get; set; }

        StaticShape PreviousSelectedObject;
        public StaticShape SelectedObject { get; set; }

        public StaticShape MovedObject { get; set; }
        public WorldPosition MovedObjectLocation { get; set; }
        BoundingBoxPrimitive MovedObjectPrimitive;

        public ConcurrentBag<StaticShape> BoundingBoxShapes = new ConcurrentBag<StaticShape>();
        readonly ConcurrentDictionary<(int tileX, int tileZ, int uid, Matrix matrix, int number), BoundingBoxPrimitive> BoundingBoxPrimitives = new ConcurrentDictionary<(int, int, int, Matrix, int), BoundingBoxPrimitive>();
        readonly ConcurrentBag<EditorPrimitive> UnusedPrimitives = new ConcurrentBag<EditorPrimitive>();
        Vector3 CrosshairPosition;

        public EditorShapes(Viewer viewer) : base(viewer, "", null, ShapeFlags.None, null, -1)
        {
            MouseCrosshair = new MouseCrosshair(Viewer, Color.GreenYellow, Color.Red, Color.Cyan);
            HandleX = new HandleX(Viewer, Color.Red);
            HandleY = new HandleY(Viewer, Color.Blue);
            HandleZ = new HandleZ(Viewer, Color.LightGreen);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (PreviousSelectedObject != SelectedObject)
            {
                if (PreviousSelectedObject?.BoundingBox != null)
                    for (var i = 0; i < PreviousSelectedObject.BoundingBox.Length; i++)
                        if (BoundingBoxPrimitives.TryRemove((PreviousSelectedObject.Location.TileX, PreviousSelectedObject.Location.TileZ, PreviousSelectedObject.Uid, PreviousSelectedObject.Location.XNAMatrix, i), out var primitive))
                            UnusedPrimitives.Add(primitive);

                PreviousSelectedObject = SelectedObject;

                if (SelectedObject?.BoundingBox?.Length > 0)
                {
                    for (var i = 0; i < SelectedObject.BoundingBox.Length; i++)
                    {
                        BoundingBoxPrimitives.TryAdd((SelectedObject.Location.TileX, SelectedObject.Location.TileZ, SelectedObject.Uid, SelectedObject.Location.XNAMatrix, i),
                            new BoundingBoxPrimitive(Viewer, SelectedObject.BoundingBox[i], Color.CornflowerBlue));
                    }
                }
            }
            for (var i = 0; i < BoundingBoxPrimitives.Count; i++)
            {
                // Sweep out the not displayable bb-s
                var bb = BoundingBoxPrimitives.Keys.ElementAtOrDefault(i);
                if (bb != default((int, int, int, Matrix, int))
                    && !BoundingBoxShapes.Any(s => s?.Location.TileX == bb.tileX && s?.Location.TileZ == bb.tileZ && s?.Uid == bb.uid)
                    && bb.tileX != SelectedObject?.Location.TileX && bb.tileZ != SelectedObject?.Location.TileZ && bb.uid != SelectedObject?.Uid)
                {
                    if (BoundingBoxPrimitives.TryRemove(bb, out var primitive))
                        UnusedPrimitives.Add(primitive);
                }
            }
            for (var j = 0; j < BoundingBoxShapes.Count; j++)
            {
                var s = BoundingBoxShapes.ElementAtOrDefault(j);
                if (s?.BoundingBox == null || s.BoundingBox.Length == 0)
                    continue;

                // Complement with all the displayable bb-s
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
            if (MovedObject == null && MovedObjectPrimitive != null)
            {
                UnusedPrimitives.Add(MovedObjectPrimitive);
                MovedObjectPrimitive = null;
            }
            else if (MovedObject != null && MovedObjectPrimitive == null)
            {
                if (MovedObject.BoundingBox?.Length > 0)
                    MovedObjectPrimitive = new BoundingBoxPrimitive(Viewer, MovedObject.BoundingBox[0], Color.MediumVioletRed);
            }
            if (MovedObjectPrimitive != null)
            {
                var dTileX = MovedObjectLocation.TileX - Viewer.Camera.TileX;
                var dTileZ = MovedObjectLocation.TileZ - Viewer.Camera.TileZ;
                var xnaDTileTranslation = MovedObjectLocation.XNAMatrix;
                xnaDTileTranslation.M41 += dTileX * 2048;
                xnaDTileTranslation.M43 -= dTileZ * 2048;
                frame.AddPrimitive(MovedObjectPrimitive.Material, MovedObjectPrimitive, RenderPrimitiveGroup.Labels, ref xnaDTileTranslation);
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
                var handleLocation = HandleLocation ?? SelectedObject?.Location;
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
    public class BoxPrimitive : EditorPrimitive
    {
        static IndexBuffer BoxIndexBuffer;

        public BoxPrimitive(Viewer viewer, float size, Color color)
            : this(viewer, new Vector3(-size / 2), new Vector3(size / 2), color)
        {
            Material = viewer.MaterialManager.Load("EditorPrimitive");
            if (BoxIndexBuffer == null)
            {
                var indexData = new short[] { 2, 3, 6, 7, 5, 3, 1, 2, 0, 6, 4, 5, 0, 1 };
                BoxIndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                BoxIndexBuffer.SetData(indexData);
            }
            IndexBuffer = BoxIndexBuffer;
            PrimitiveCount = IndexBuffer.IndexCount - 2;
            PrimitiveType = PrimitiveType.TriangleStrip;
        }

        protected BoxPrimitive(Viewer viewer, Vector3 min, Vector3 max, Color color)
        {
            var vertexData = new VertexPositionColor[]
            {
                new VertexPositionColor(min, color),
                new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), color),
                new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), color),
                new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), color),
                new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), color),
                new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), color),
                new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), color),
                new VertexPositionColor(max, color)
            };
            VertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), vertexData.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertexData);
        }
    }

    [CallOnThread("Loader")]
    public class BoundingBoxPrimitive : BoxPrimitive
    {
        static IndexBuffer BoundingBoxIndexBuffer;
        public readonly Matrix ComplexTransform;

        public BoundingBoxPrimitive(Viewer viewer, BoundingBox boundingBox, Color color)
            : this(viewer, boundingBox.Min, boundingBox.Max, color)
        {
            ComplexTransform = boundingBox.ComplexTransform;
        }

        public BoundingBoxPrimitive(Viewer viewer, Vector3 min, Vector3 max, Color color)
            : base(viewer, min, max, color)
        {
            Material = viewer.MaterialManager.Load("EditorPrimitive");
            ComplexTransform = Matrix.Identity;
            if (BoundingBoxIndexBuffer == null)
            {
                var indexData = new short[] { 0, 1, 0, 2, 0, 4, 1, 3, 1, 5, 2, 3, 2, 6, 3, 7, 4, 5, 4, 6, 5, 7, 6, 7 };
                BoundingBoxIndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                BoundingBoxIndexBuffer.SetData(indexData);
            }
            IndexBuffer = BoundingBoxIndexBuffer;
            PrimitiveCount = IndexBuffer.IndexCount / 2;
            PrimitiveType = PrimitiveType.LineList;
        }
    }

    [CallOnThread("Loader")]
    public class MouseCrosshair : EditorPrimitive
    {
        public MouseCrosshair(Viewer viewer, Color color, Color northColor, Color southColor)
        {
            var vertexData = new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(-5, 0, 0), color),
                new VertexPositionColor(new Vector3(5, 0, 0), color),
                new VertexPositionColor(new Vector3(0, 0, -5), northColor),
                new VertexPositionColor(new Vector3(0, 0, 5), southColor),
                new VertexPositionColor(new Vector3(0, -5, 0), color),
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
            PrimitiveCount = VertexBuffer.VertexCount - 2;
            PrimitiveType = PrimitiveType.TriangleStrip;
            Material = viewer.MaterialManager.Load("EditorPrimitive");
        }

        protected virtual VertexPositionColor[] GetVertexData(Color color) => GetVertexData(0, 1, 2, color);
        protected VertexPositionColor[] GetVertexData(int x, int y, int z, Color color)
        {
            var l = 5f; // total length, meter
            var d = 0.1f; // shaft half thickness
            var a = l / 5; // arrow head length
            var b = a / 4; // arrow head half thickness
            var c = l - a;
            var data = new float[][]
            {
                // Arrow shaft
                new[] { 0, d, 0 },
                new[] { c, d, 0 },
                new[] { 0, 0, d },
                new[] { c, 0, d },
                new[] { 0, -d, 0 },
                new[] { c, -d, 0 },
                new[] { 0, 0, -d },
                new[] { c, 0, -d },

                // Arrow head
                new[] { l, 0, 0 },
                new[] { c, +b, +b },
                new[] { c, -b, +b },
                new[] { c, -b, -b },
                new[] { l, 0, 0 },
                new[] { c, +b, -b },
                new[] { c, +b, +b },
                new[] { c, -b, -b },
            };
            return data.Select(v => new VertexPositionColor(new Vector3(v[x], v[y], v[z]), color)).ToArray();
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
